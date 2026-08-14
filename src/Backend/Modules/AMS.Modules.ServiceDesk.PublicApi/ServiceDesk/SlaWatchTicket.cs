namespace AMS.Modules.ServiceDesk.PublicApi.ServiceDesk;

/// <summary>An open ticket, as the SLA monitor needs to see it.</summary>
/// <param name="Id">The ticket.</param>
/// <param name="RequestNumber">What the requester quotes, and what an escalation says.</param>
/// <param name="Subject">The one-line summary.</param>
/// <param name="Priority">Low, Medium, High or Critical.</param>
/// <param name="StatusName">Where it is, for the message.</param>
/// <param name="SlaPolicyId">Which policy judges it. Never null here — a ticket without one is not watched.</param>
/// <param name="LocationId">The branch, whose calendar decides what a minute is.</param>
/// <param name="ResponseDueOnUtc">When somebody must have replied by.</param>
/// <param name="ResolutionDueOnUtc">When it must be fixed by.</param>
/// <param name="FirstResponseOnUtc">
/// When somebody did. Null means the response clock is still running, and it is
/// the only thing that makes a response escalation worth sending.
/// </param>
/// <param name="IsSlaPaused">
/// Whether the clock is frozen by the ticket's current status. A paused ticket
/// is not late; it is waiting on somebody who is not us.
/// </param>
/// <param name="AssignedToUserId">The technician holding it, if any.</param>
/// <param name="AssignedTeamId">The team it sits with, if any.</param>
/// <param name="RequestedByEmployeeId">Who asked. Their manager is a possible recipient.</param>
public sealed record SlaWatchTicket(
    int Id,
    string RequestNumber,
    string Subject,
    string Priority,
    string StatusName,
    int SlaPolicyId,
    int? LocationId,
    DateTime? ResponseDueOnUtc,
    DateTime? ResolutionDueOnUtc,
    DateTime? FirstResponseOnUtc,
    bool IsSlaPaused,
    int? AssignedToUserId,
    int? AssignedTeamId,
    int RequestedByEmployeeId);

/// <summary>
/// The tickets the SLA monitor watches, and the one thing it writes back.
/// </summary>
/// <remarks>
/// <para>
/// Narrow on purpose. ServiceLevel owns the rule about when a target is missed
/// and who is told; ServiceDesk owns the tickets. Neither can do the job alone,
/// and the alternative — either module reading the other's tables — is the
/// coupling schema-per-module exists to remove.
/// </para>
/// <para>
/// The write is a single sentence in the ticket's own timeline, so somebody
/// reading the ticket can see that an escalation went out without going to
/// another screen for it. Nothing here can change a ticket's state; escalating
/// is telling people, not reassigning work.
/// </para>
/// </remarks>
public interface ISlaWatchList
{
    /// <summary>
    /// Every open ticket that has a policy and at least one due date.
    /// </summary>
    /// <remarks>
    /// Closed tickets are excluded, and so are tickets with no policy: neither
    /// can be late. The monitor filters further — a paused clock, a response
    /// already given — but the ones that can never matter are left here, where
    /// the index is.
    /// </remarks>
    Task<IReadOnlyList<SlaWatchTicket>> OpenTicketsAsync(CancellationToken ct);

    /// <summary>Who leads a support team, so an escalation can reach them.</summary>
    Task<IReadOnlyList<int>> TeamLeadsAsync(int supportTeamId, CancellationToken ct);

    /// <summary>Records that an escalation went out, in the ticket's own timeline.</summary>
    Task NoteEscalationAsync(int ticketId, string text, CancellationToken ct);
}
