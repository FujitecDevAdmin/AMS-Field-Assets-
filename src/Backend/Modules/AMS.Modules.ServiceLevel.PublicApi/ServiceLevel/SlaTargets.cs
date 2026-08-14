namespace AMS.Modules.ServiceLevel.PublicApi.ServiceLevel;

/// <summary>What a ticket has to meet, and by when.</summary>
/// <param name="SlaPolicyId">The policy that decided it.</param>
/// <param name="PolicyName">Its name, so a screen need not ask twice.</param>
/// <param name="StartOnUtc">
/// When the clock actually starts. Not the same as when the ticket was raised:
/// a ticket logged at ten at night, or inside the branch's final minutes,
/// starts when the branch next opens.
/// </param>
/// <param name="ResponseDueOnUtc">When somebody must have replied by.</param>
/// <param name="ResolutionDueOnUtc">When it must be fixed by.</param>
/// <param name="IsScheduledHold">
/// True when the clock has not started yet, because the branch is shut. The
/// ticket is not late; it is waiting for Monday.
/// </param>
/// <param name="ScheduleHoldReason">The sentence shown to the requester.</param>
public sealed record SlaTargets(
    int SlaPolicyId,
    string PolicyName,
    DateTime StartOnUtc,
    DateTime? ResponseDueOnUtc,
    DateTime? ResolutionDueOnUtc,
    bool IsScheduledHold,
    string? ScheduleHoldReason);

/// <summary>Everything the calculator needs about a ticket.</summary>
/// <param name="Priority">Low, Medium, High or Critical. Chooses the policy.</param>
/// <param name="LocationId">
/// The branch, whose calendar decides what a minute is. Null means the working
/// week nobody configured: Monday to Friday.
/// </param>
/// <param name="RaisedOnUtc">When the ticket arrived.</param>
public sealed record SlaTargetRequest(string Priority, int? LocationId, DateTime RaisedOnUtc);

/// <summary>
/// What "on time" means for a ticket, and how many minutes have actually
/// passed.
/// </summary>
/// <remarks>
/// <para>
/// The one thing ServiceDesk needs from this module, and the reason the module
/// is separate: the calendar is a property of the BRANCH, not of the ticket.
/// </para>
/// <para>
/// Read-only. Another module may ask what a policy implies; none of them may
/// create or edit one, because a target quietly changed by the module it
/// judges is not a target.
/// </para>
/// </remarks>
public interface ISlaCalculator
{
    /// <summary>
    /// The targets for a ticket, or null when no active policy covers its
    /// priority.
    /// </summary>
    /// <remarks>
    /// Null is an ordinary answer, not a failure. A site that has not
    /// configured SLA policies still raises tickets; they simply have no due
    /// date, and a ticket with no due date is never overdue.
    /// </remarks>
    Task<SlaTargets?> ComputeTargetsAsync(SlaTargetRequest request, CancellationToken ct);

    /// <summary>
    /// The operational minutes between two instants at a branch.
    /// </summary>
    /// <param name="locationId">The branch. Null uses the default working week.</param>
    /// <param name="fromUtc">The start of the span.</param>
    /// <param name="toUtc">The end of it.</param>
    /// <param name="slaPolicyId">
    /// The policy in force, whose Respect* flags decide whether the calendar
    /// applies at all. Null counts wall-clock minutes — which is what a ticket
    /// with no policy gets.
    /// </param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// This is what makes "a ticket held over a weekend consumes nothing" true
    /// rather than aspirational.
    /// </remarks>
    Task<int> OperationalMinutesAsync(
        int? locationId,
        DateTime fromUtc,
        DateTime toUtc,
        int? slaPolicyId,
        CancellationToken ct);
}
