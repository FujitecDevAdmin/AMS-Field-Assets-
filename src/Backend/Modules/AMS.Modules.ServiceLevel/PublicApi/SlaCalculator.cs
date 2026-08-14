using AMS.Modules.ServiceLevel.Calendar;
using AMS.Modules.ServiceLevel.Domain;
using AMS.Modules.ServiceLevel.Persistence;
using AMS.Modules.ServiceLevel.PublicApi.ServiceLevel;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.PublicApi;

/// <summary>
/// Turns a priority and a branch into a pair of due dates, and a span into
/// operational minutes.
/// </summary>
/// <remarks>
/// The whole module reduced to two questions, which is what
/// <see cref="ISlaCalculator"/> exists to expose. Everything hard is in
/// <see cref="OperationalCalendar"/>; this class chooses a policy, reads its
/// Respect* flags, and asks.
/// </remarks>
public sealed class SlaCalculator(ServiceLevelDbContext db, CalendarLoader calendars)
    : ISlaCalculator
{
    public async Task<SlaTargets?> ComputeTargetsAsync(
        SlaTargetRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var policy = await ActivePolicyAsync(request.Priority, ct);

        // No policy for this priority is an ordinary answer. A site that has
        // not configured SLA still raises tickets; they have no due date, and a
        // ticket with no due date is never overdue.
        if (policy is null)
        {
            return null;
        }

        var calendar = await calendars.LoadAsync(request.LocationId ?? 0, ct);

        if (!SlaCalendar.RespectsCalendar(policy))
        {
            // A Critical policy usually ignores the calendar entirely: a
            // production outage does not wait for Monday. Wall-clock minutes,
            // and no scheduled hold.
            return new SlaTargets(
                policy.Id,
                policy.PolicyName,
                request.RaisedOnUtc,
                request.RaisedOnUtc.AddMinutes(policy.ResponseTargetMinutes),
                request.RaisedOnUtc.AddMinutes(policy.ResolutionTargetMinutes),
                IsScheduledHold: false,
                ScheduleHoldReason: null);
        }

        var applied = SlaCalendar.AsSeenBy(calendar, policy);

        var start = OperationalCalendar.NextOperationalStart(
            applied, request.RaisedOnUtc, policy.RespectOperationalHours);

        if (start is null)
        {
            // The branch never opens again, which is a calendar somebody has
            // misconfigured. Targets computed from it would be fiction, so
            // there are none; the ticket is still raised.
            return null;
        }

        var held = start > request.RaisedOnUtc;

        return new SlaTargets(
            policy.Id,
            policy.PolicyName,
            start.Value,
            OperationalCalendar.AddOperationalMinutes(
                applied, start.Value, policy.ResponseTargetMinutes),
            OperationalCalendar.AddOperationalMinutes(
                applied, start.Value, policy.ResolutionTargetMinutes),
            held,
            held ? Reason(applied, request.RaisedOnUtc, start.Value) : null);
    }

    public async Task<int> OperationalMinutesAsync(
        int? locationId,
        DateTime fromUtc,
        DateTime toUtc,
        int? slaPolicyId,
        CancellationToken ct)
    {
        if (toUtc <= fromUtc)
        {
            return 0;
        }

        var policy = slaPolicyId is { } id
            ? await db.SlaPolicies.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct)
            : null;

        // No policy, or one that ignores the calendar: wall clock. Returning
        // zero instead would make a ticket with no policy consume nothing and
        // look permanently untouched.
        if (policy is null || !SlaCalendar.RespectsCalendar(policy))
        {
            return (int)(toUtc - fromUtc).TotalMinutes;
        }

        var calendar = await calendars.LoadAsync(locationId ?? 0, ct);

        return OperationalCalendar.OperationalMinutesBetween(
            SlaCalendar.AsSeenBy(calendar, policy), fromUtc, toUtc);
    }

    /// <summary>The live policy for a priority, if there is one.</summary>
    /// <remarks>
    /// <c>UX_SlaPolicy_ActivePriority</c> allows exactly one, so this cannot
    /// pick the wrong of two — which is the whole reason that index exists.
    /// </remarks>
    private async Task<SlaPolicy?> ActivePolicyAsync(string priority, CancellationToken ct) =>
        await db.SlaPolicies
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.IsActive && p.Priority == priority, ct);

    /// <summary>The sentence shown to a requester whose clock has not started.</summary>
    private static string Reason(CalendarSnapshot calendar, DateTime raisedUtc, DateTime startUtc)
    {
        var raised = TimeZoneInfo.ConvertTimeFromUtc(raisedUtc, calendar.TimeZone);
        var start = TimeZoneInfo.ConvertTimeFromUtc(startUtc, calendar.TimeZone);

        return raised.Date == start.Date
            ? $"Raised outside working hours. The clock starts at {start:HH:mm}."
            : $"Raised when the branch was closed. The clock starts on {start:dddd d MMMM} at {start:HH:mm}.";
    }
}
