using AMS.Modules.Identity.PublicApi.Identity;
using AMS.Modules.Notifications.PublicApi.Notifications;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.Modules.ServiceDesk.PublicApi.ServiceDesk;
using AMS.Modules.ServiceLevel.Calendar;
using AMS.Modules.ServiceLevel.Domain;
using AMS.Modules.ServiceLevel.Persistence;
using AMS.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Escalation;

/// <summary>
/// Tells people when a target has been missed.
/// </summary>
/// <remarks>
/// <para>
/// The last thing the schema was written for and nothing used.
/// <c>SlaEscalation</c> holds the ladder, <c>SlaEscalationLog</c> holds the
/// evidence, and <c>UX_SlaEscalationLog_OncePerLevel</c> — the design script's
/// own note says it plainly — exists because "the monitor runs every minute,
/// and without it a ticket that stays overdue for a day sends 1,440 e-mails
/// and everybody filters the address".
/// </para>
/// <para>
/// It needs two modules' worth of knowledge: the tickets are ServiceDesk's and
/// the rules are this module's. Neither can do the job alone, so it lives here
/// — where the ladder and the calendar are — and reaches for tickets through
/// <see cref="ISlaWatchList"/>.
/// </para>
/// </remarks>
public sealed class SlaEscalationMonitor(
    ServiceLevelDbContext db,
    ISlaWatchList tickets,
    INotifier notifier,
    IUserDirectory users,
    IEmployeeDirectory employees,
    CalendarLoader calendars,
    IClock clock)
{
    /// <summary>
    /// The capability that stands for "branch administrator".
    /// </summary>
    /// <remarks>
    /// <c>SlaEscalation</c> has a <c>RecipientType</c> of BranchAdmin but no
    /// capability column to say which capability makes somebody one — unlike
    /// the approval workflow's <c>LocationBranchAdmin</c> rule, which carries
    /// its own. So the module picks the capability that defines the role FOR
    /// TICKETS: whoever may work them at that branch is who an unworked ticket
    /// escalates to.
    /// </remarks>
    private const string BranchAdminCapability = "request.manage";

    /// <summary>
    /// Fires everything that is due. Returns how many escalations went out.
    /// </summary>
    /// <remarks>
    /// Callable directly, so it can be tested by moving a clock rather than by
    /// waiting. A worker that only runs on a timer is a worker nobody can test.
    /// </remarks>
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var watched = await tickets.OpenTicketsAsync(ct);

        if (watched.Count == 0)
        {
            return 0;
        }

        var policyIds = watched.Select(t => t.SlaPolicyId).Distinct().ToList();

        var policies = await db.SlaPolicies
            .AsNoTracking()
            .Where(p => policyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var ladders = (await db.SlaEscalations
                .AsNoTracking()
                .Where(e => policyIds.Contains(e.SlaPolicyId) && e.IsEnabled)
                .OrderBy(e => e.Level)
                .ToListAsync(ct))
            .GroupBy(e => e.SlaPolicyId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var now = clock.UtcNow;
        var fired = 0;

        foreach (var ticket in watched)
        {
            // A paused clock is not a late ticket. It is waiting on somebody
            // who is not us, and escalating it would be telling a technician
            // off for a delay the requester is causing.
            if (ticket.IsSlaPaused)
            {
                continue;
            }

            if (!policies.TryGetValue(ticket.SlaPolicyId, out var policy)
                || !ladders.TryGetValue(ticket.SlaPolicyId, out var ladder))
            {
                continue;
            }

            foreach (var rung in ladder)
            {
                if (await FireIfDueAsync(ticket, policy, rung, now, ct))
                {
                    fired++;
                }
            }
        }

        return fired;
    }

    private async Task<bool> FireIfDueAsync(
        SlaWatchTicket ticket,
        SlaPolicy policy,
        SlaEscalation rung,
        DateTime now,
        CancellationToken ct)
    {
        var response = rung.EscalationType == EscalationType.Response;

        // A response escalation about a ticket somebody has already answered is
        // a complaint about a thing that did not happen.
        if (response && ticket.FirstResponseOnUtc is not null)
        {
            return false;
        }

        var due = response ? ticket.ResponseDueOnUtc : ticket.ResolutionDueOnUtc;

        if (due is not { } dueOnUtc)
        {
            return false;
        }

        var fireAt = await FireTimeAsync(ticket, policy, rung, dueOnUtc, ct);

        if (fireAt is not { } when || now < when)
        {
            return false;
        }

        // UX_SlaEscalationLog_OncePerLevel would catch a repeat, but as a 409
        // inside a background pass — which nobody sees. Asking first means the
        // ordinary case is a read, and the index is the backstop it should be.
        if (await db.SlaEscalationLogs.AnyAsync(
                l => l.ServiceRequestId == ticket.Id
                    && l.SlaEscalationId == rung.Id
                    && l.Outcome != EscalationOutcome.Failed, ct))
        {
            return false;
        }

        var recipients = await ResolveAsync(ticket, rung, ct);

        await SendAsync(ticket, rung, recipients, now, ct);

        return true;
    }

    /// <summary>
    /// When this rung fires: the due time plus the extra the threshold asks
    /// for, in operational minutes.
    /// </summary>
    /// <remarks>
    /// <c>ThresholdPercent</c> is ADDITIVE to the target — 100 means at the due
    /// time, 150 means half the target again past it — which is what makes one
    /// ladder usable by policies with different targets.
    ///
    /// The extra is measured in the branch's operational minutes, like the
    /// target it is a percentage of. Measuring the target in working hours and
    /// the grace period in wall clock would make a Friday-afternoon breach
    /// escalate over the weekend.
    /// </remarks>
    private async Task<DateTime?> FireTimeAsync(
        SlaWatchTicket ticket,
        SlaPolicy policy,
        SlaEscalation rung,
        DateTime dueOnUtc,
        CancellationToken ct)
    {
        if (rung.ThresholdPercent <= 100)
        {
            return dueOnUtc;
        }

        var target = rung.EscalationType == EscalationType.Response
            ? policy.ResponseTargetMinutes
            : policy.ResolutionTargetMinutes;

        var extra = (int)((long)target * (rung.ThresholdPercent - 100) / 100);

        if (extra <= 0)
        {
            return dueOnUtc;
        }

        if (!SlaCalendar.RespectsCalendar(policy))
        {
            return dueOnUtc.AddMinutes(extra);
        }

        var calendar = await calendars.LoadAsync(ticket.LocationId ?? 0, ct);

        return OperationalCalendar.AddOperationalMinutes(
            SlaCalendar.AsSeenBy(calendar, policy), dueOnUtc, extra);
    }

    /// <summary>Who this rung tells, as addresses and user ids.</summary>
    private async Task<Recipients> ResolveAsync(
        SlaWatchTicket ticket,
        SlaEscalation rung,
        CancellationToken ct) => rung.RecipientType switch
    {
        EscalationRecipient.AssignedTechnician =>
            await FromUsersAsync(ticket.AssignedToUserId is { } id ? [id] : [], ct),

        EscalationRecipient.TeamLead => await FromUsersAsync(
            ticket.AssignedTeamId is { } team
                ? await tickets.TeamLeadsAsync(team, ct)
                : [],
            ct),

        EscalationRecipient.BranchAdmin => From(
            await users.WithCapabilityAsync(BranchAdminCapability, ticket.LocationId, ct)),

        EscalationRecipient.Manager => await ManagerAsync(ticket, ct),

        // The address IS the recipient. A distribution list, a vendor, a duty
        // phone that turns messages into pages.
        EscalationRecipient.Custom => new Recipients(
            rung.RecipientAddress is { Length: > 0 } address ? [address] : [], []),

        _ => new Recipients([], []),
    };

    private async Task<Recipients> ManagerAsync(SlaWatchTicket ticket, CancellationToken ct)
    {
        var managerId = await employees.ManagerOfAsync(ticket.RequestedByEmployeeId, ct);

        if (managerId is not { } manager)
        {
            return new Recipients([], []);
        }

        var contact = await users.ForEmployeeAsync(manager, ct);

        return contact is null ? new Recipients([], []) : From([contact]);
    }

    private async Task<Recipients> FromUsersAsync(
        IReadOnlyList<int> userIds,
        CancellationToken ct)
    {
        var contacts = new List<UserContact>();

        foreach (var userId in userIds)
        {
            if (await users.FindAsync(userId, ct) is { } contact)
            {
                contacts.Add(contact);
            }
        }

        return From(contacts);
    }

    private static Recipients From(IReadOnlyList<UserContact> contacts) =>
        new(
            [.. contacts
                .Where(c => !string.IsNullOrWhiteSpace(c.Email))
                .Select(c => c.Email!)
                .Distinct(StringComparer.OrdinalIgnoreCase)],
            [.. contacts.Select(c => c.UserId).Distinct()]);

    private async Task SendAsync(
        SlaWatchTicket ticket,
        SlaEscalation rung,
        Recipients recipients,
        DateTime now,
        CancellationToken ct)
    {
        var what = rung.EscalationType == EscalationType.Response ? "response" : "resolution";

        var subject =
            $"SLA {what} overdue (level {rung.Level}): {ticket.RequestNumber} — {ticket.Subject}";

        var body = string.Join(
            Environment.NewLine,
            $"The {what} target for this ticket has been missed.",
            string.Empty,
            $"Request:  {ticket.RequestNumber}",
            $"Subject:  {ticket.Subject}",
            $"Priority: {ticket.Priority}",
            $"Status:   {ticket.StatusName}");

        var wantsEmail = rung.Channel is EscalationChannel.Email or EscalationChannel.Both;
        var wantsInApp = rung.Channel is EscalationChannel.InApp or EscalationChannel.Both;

        if (wantsInApp && recipients.UserIds.Count > 0)
        {
            await notifier.NotifyManyAsync(
                recipients.UserIds,
                $"{ticket.RequestNumber} has missed its {what} target.",
                $"/service-desk/requests/{ticket.Id}",
                ct);
        }

        if (recipients.Addresses.Count == 0)
        {
            // Nobody to tell. A Skipped row rather than silence: the rung is
            // configured and did not fire, which is a configuration problem
            // somebody has to see. It also blocks a repeat, so the monitor does
            // not rediscover the same empty rung every minute.
            db.SlaEscalationLogs.Add(Log(
                ticket, rung, "(nobody could be resolved)", EscalationOutcome.Skipped,
                "No recipient could be found for this escalation level.", null, now));

            await db.SaveChangesAsync(ct);

            return;
        }

        long? outboxId = null;

        if (wantsEmail)
        {
            outboxId = await notifier.QueueEmailAsync(
                new OutboundEmail(
                    string.Join(';', recipients.Addresses),
                    null,
                    subject,
                    body,
                    IsHtml: false,
                    EmailSource.SlaEscalation,
                    ticket.Id),
                ct);
        }

        db.SlaEscalationLogs.Add(Log(
            ticket,
            rung,
            string.Join(';', recipients.Addresses),
            wantsEmail ? EscalationOutcome.Queued : EscalationOutcome.Sent,
            null,
            outboxId,
            now));

        await db.SaveChangesAsync(ct);

        await tickets.NoteEscalationAsync(
            ticket.Id,
            $"SLA {what} escalation level {rung.Level} sent to "
            + $"{string.Join(", ", recipients.Addresses)}.",
            ct);
    }

    private static SlaEscalationLog Log(
        SlaWatchTicket ticket,
        SlaEscalation rung,
        string sentTo,
        string outcome,
        string? failureReason,
        long? outboxId,
        DateTime now) => new()
        {
            ServiceRequestId = ticket.Id,
            SlaEscalationId = rung.Id,
            EscalationType = rung.EscalationType,
            Level = rung.Level,
            SentTo = sentTo,
            Channel = rung.Channel,
            EmailOutboxId = outboxId,
            Outcome = outcome,
            FailureReason = failureReason,
            FiredOnUtc = now,
        };

    /// <summary>Where an escalation goes: addresses to write to, accounts to notify.</summary>
    private sealed record Recipients(
        IReadOnlyList<string> Addresses,
        IReadOnlyList<int> UserIds);
}
