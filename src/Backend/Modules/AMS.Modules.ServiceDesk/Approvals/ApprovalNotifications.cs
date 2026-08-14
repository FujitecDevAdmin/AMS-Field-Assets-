using AMS.Modules.Identity.PublicApi.Identity;
using AMS.Modules.Notifications.PublicApi.Notifications;
using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Approvals;

/// <summary>What an approval notification is about. CK_ApprovalNotificationLog_Type.</summary>
public static class ApprovalNotificationType
{
    /// <summary>Somebody is being asked to approve something.</summary>
    public const string ApprovalRequired = "ApprovalRequired";

    /// <summary>They were asked a while ago and have not answered.</summary>
    public const string Reminder = "Reminder";

    /// <summary>They still have not, and it has gone up.</summary>
    public const string Escalation = "Escalation";

    /// <summary>A level was approved and the run has moved on.</summary>
    public const string StepApproved = "StepApproved";

    /// <summary>The whole thing was approved.</summary>
    public const string RequestApproved = "RequestApproved";

    /// <summary>It was rejected.</summary>
    public const string RequestRejected = "RequestRejected";

    /// <summary>It was called off.</summary>
    public const string RequestCancelled = "RequestCancelled";
}

/// <summary>How far an approval notification has got. CK_ApprovalNotificationLog_Status.</summary>
public static class ApprovalNotificationStatus
{
    public const string Queued = "Queued";
    public const string Sent = "Sent";
    public const string Failed = "Failed";

    /// <summary>Deliberately not sent — nobody had an address, say.</summary>
    public const string Skipped = "Skipped";
}

/// <summary>
/// Tells people about approvals, and records that it did.
/// </summary>
/// <remarks>
/// <para>
/// The gap this closes: <c>SubmitForApproval</c> resolved approvers and wrote
/// them down, and nothing ever told them. An approval waiting on somebody who
/// does not know is an approval that waits for ever.
/// </para>
/// <para>
/// Every message goes through the Notifications outbox — a dead SMTP host
/// retries instead of losing it — and every one leaves a row in
/// <c>ApprovalNotificationLog</c> saying why it was queued and what became of
/// it. That table is the answer to "nobody told me", which is the only question
/// anybody asks about an approval that went wrong.
/// </para>
/// <para>
/// The log row is written even when nothing was sent. Somebody with no address
/// gets a <c>Skipped</c> row, because "we did not tell them" is a fact worth
/// having and an empty log is indistinguishable from a worker that never ran.
/// </para>
/// <para>
/// In practice that only happens to people looked up at the time - the
/// submitter of a run. A PARTICIPANT always has an address: the resolver drops
/// anybody without one at submission, precisely so a level cannot end up
/// waiting on somebody who could not be asked, and
/// <c>CK_RequestApprovalParticipant_Identity</c> requires the snapshot to be
/// non-empty.
/// </para>
/// </remarks>
public sealed class ApprovalNotifications(
    ServiceDeskDbContext db,
    INotifier notifier,
    IUserDirectory users,
    IClock clock)
{
    /// <summary>Asks a level's approvers to approve.</summary>
    public async Task AskAsync(
        RequestApprovalInstance instance,
        RequestApprovalStep step,
        ServiceRequest ticket,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(ticket);

        var participants = await db.RequestApprovalParticipants
            .Where(p => p.RequestApprovalStepId == step.Id
                && p.ParticipantStatus == ParticipantStatus.Pending)
            .ToListAsync(ct);

        foreach (var participant in participants)
        {
            await SendAsync(
                instance,
                step,
                participant,
                ApprovalNotificationType.ApprovalRequired,
                $"Approval needed: {ticket.RequestNumber} — {ticket.Subject}",
                Body(ticket, step, "Your approval is needed."),
                occurrence: 0,
                ct);
        }
    }

    /// <summary>Nudges a level's approvers, or escalates past them.</summary>
    public async Task ChaseAsync(
        RequestApprovalInstance instance,
        RequestApprovalStep step,
        ServiceRequest ticket,
        string type,
        int occurrence,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(ticket);

        if (type == ApprovalNotificationType.Escalation)
        {
            // Up, not sideways. The stage's timer says when to escalate but not
            // to whom — the schema has no recipient rule for approvals, unlike
            // SlaEscalation — so it goes to whoever submitted the request. They
            // asked for it, they are waiting on it, and they are the one person
            // certain to care that it has stalled.
            await EscalateToSubmitterAsync(instance, step, ticket, occurrence, ct);

            return;
        }

        var participants = await db.RequestApprovalParticipants
            .Where(p => p.RequestApprovalStepId == step.Id
                && p.ParticipantStatus == ParticipantStatus.Pending)
            .ToListAsync(ct);

        foreach (var participant in participants)
        {
            await SendAsync(
                instance,
                step,
                participant,
                type,
                $"Still waiting: {ticket.RequestNumber} — {ticket.Subject}",
                Body(ticket, step, "This is still waiting for your approval."),
                occurrence,
                ct);
        }
    }

    /// <summary>Tells the person who asked how it ended.</summary>
    public async Task AnnounceAsync(
        RequestApprovalInstance instance,
        ServiceRequest ticket,
        string type,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(ticket);

        var submitter = await users.FindAsync(instance.SubmittedByUserId, ct);

        var outcome = type switch
        {
            ApprovalNotificationType.RequestApproved => "approved",
            ApprovalNotificationType.RequestRejected => "rejected",
            _ => "cancelled",
        };

        var subject = $"Request {outcome}: {ticket.RequestNumber} — {ticket.Subject}";

        await notifier.NotifyAsync(
            instance.SubmittedByUserId,
            $"{ticket.RequestNumber} was {outcome}.",
            $"/service-desk/requests/{ticket.Id}",
            ct);

        await RecordAsync(
            instance,
            step: null,
            participant: null,
            type,
            subject,
            submitter?.Email,
            body: Body(ticket, step: null, $"This request was {outcome}."),
            occurrence: 0,
            ct);
    }

    /// <summary>Tells a level's approvers that somebody else settled it.</summary>
    public async Task AnnounceStepApprovedAsync(
        RequestApprovalInstance instance,
        RequestApprovalStep step,
        ServiceRequest ticket,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(ticket);

        // Everybody who was asked and never answered, so an approval sitting in
        // their list stops asking for a decision that can no longer change
        // anything. Only in-app: a second e-mail saying "never mind" is how
        // people learn to filter the first one.
        var others = await db.RequestApprovalParticipants
            .Where(p => p.RequestApprovalStepId == step.Id
                && p.ParticipantStatus == ParticipantStatus.Cancelled
                && p.ApproverUserId != null)
            .Select(p => p.ApproverUserId!.Value)
            .ToListAsync(ct);

        if (others.Count == 0)
        {
            return;
        }

        await notifier.NotifyManyAsync(
            others,
            $"{step.StageNameSnapshot} on {ticket.RequestNumber} was settled by somebody else.",
            $"/service-desk/requests/{ticket.Id}",
            ct);
    }

    /// <summary>Tells the people who were waiting that they no longer are.</summary>
    /// <remarks>
    /// In-app only. An e-mail saying "never mind" is how people learn to filter
    /// the one that asked.
    /// </remarks>
    public async Task NotifyCancelledAsync(
        IReadOnlyList<int> userIds,
        ServiceRequest ticket,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        await notifier.NotifyManyAsync(
            userIds,
            $"The approval for {ticket.RequestNumber} was cancelled.",
            $"/service-desk/requests/{ticket.Id}",
            ct);
    }

    private async Task EscalateToSubmitterAsync(
        RequestApprovalInstance instance,
        RequestApprovalStep step,
        ServiceRequest ticket,
        int occurrence,
        CancellationToken ct)
    {
        var submitter = await users.FindAsync(instance.SubmittedByUserId, ct);

        await notifier.NotifyAsync(
            instance.SubmittedByUserId,
            $"{ticket.RequestNumber} is stuck at {step.StageNameSnapshot}.",
            $"/service-desk/requests/{ticket.Id}",
            ct);

        await RecordAsync(
            instance,
            step,
            participant: null,
            ApprovalNotificationType.Escalation,
            $"Approval overdue: {ticket.RequestNumber} — {ticket.Subject}",
            submitter?.Email,
            Body(ticket, step, "This approval has passed its due time and nobody has acted."),
            occurrence,
            ct);
    }

    private async Task SendAsync(
        RequestApprovalInstance instance,
        RequestApprovalStep step,
        RequestApprovalParticipant participant,
        string type,
        string subject,
        string body,
        int occurrence,
        CancellationToken ct)
    {
        if (participant.ApproverUserId is { } userId)
        {
            await notifier.NotifyAsync(
                userId,
                subject,
                $"/service-desk/approvals/{participant.Id}",
                ct);
        }

        await RecordAsync(
            instance, step, participant, type, subject,
            participant.ApproverEmailSnapshot, body, occurrence, ct);
    }

    /// <summary>
    /// Queues the message and writes the log row, or records why it was not
    /// sent.
    /// </summary>
    private async Task RecordAsync(
        RequestApprovalInstance instance,
        RequestApprovalStep? step,
        RequestApprovalParticipant? participant,
        string type,
        string subject,
        string? recipient,
        string body,
        int occurrence,
        CancellationToken ct)
    {
        // Derived from what the message IS, so a worker that restarts mid-pass
        // collides on the index rather than sending everybody the same thing
        // again.
        var key = DeterministicGuid.From(
            type, instance.Id, step?.Id, participant?.Id, occurrence);

        if (await db.ApprovalNotificationLogs.AnyAsync(l => l.IdempotencyKey == key, ct))
        {
            return;
        }

        var now = clock.UtcNow;
        var hasAddress = !string.IsNullOrWhiteSpace(recipient);

        long? outboxId = null;

        if (hasAddress)
        {
            outboxId = await notifier.QueueEmailAsync(
                new OutboundEmail(
                    recipient!, null, subject, body, IsHtml: false,
                    EmailSource.Approval, instance.Id),
                ct);
        }

        db.ApprovalNotificationLogs.Add(new ApprovalNotificationLog
        {
            RequestApprovalInstanceId = instance.Id,
            RequestApprovalStepId = step?.Id,
            RequestApprovalParticipantId = participant?.Id,
            NotificationType = type,
            IdempotencyKey = key,
            // The column is NOT NULL, and a row saying an address was missing
            // is more use than no row at all.
            RecipientAddress = hasAddress ? recipient! : "(no address on file)",
            SubjectSnapshot = subject,
            EmailOutboxId = outboxId,
            // Queued means it is in the outbox. Skipped means nobody could be
            // written to — a fact worth recording, because an empty log is
            // indistinguishable from a worker that never ran.
            Status = hasAddress
                ? ApprovalNotificationStatus.Queued
                : ApprovalNotificationStatus.Skipped,
            AttemptCount = hasAddress ? 1 : 0,
            LastError = hasAddress ? null : "No e-mail address was recorded for this approver.",
            QueuedOnUtc = now,
        });

        await db.SaveChangesAsync(ct);
    }

    private static string Body(ServiceRequest ticket, RequestApprovalStep? step, string opening)
    {
        var lines = new List<string>
        {
            opening,
            string.Empty,
            $"Request: {ticket.RequestNumber}",
            $"Subject: {ticket.Subject}",
            $"Priority: {ticket.Priority}",
        };

        if (step is not null)
        {
            lines.Add($"Stage:   {step.StageNameSnapshot}");

            if (step.DueOnUtc is { } due)
            {
                lines.Add($"Due:     {due:yyyy-MM-dd HH:mm} UTC");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}
