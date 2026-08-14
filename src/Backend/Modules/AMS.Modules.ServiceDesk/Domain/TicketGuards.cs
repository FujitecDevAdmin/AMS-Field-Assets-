using AMS.SharedKernel.Results;

namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>Rules every write to a ticket shares.</summary>
public static class TicketGuards
{
    /// <summary>
    /// A closed ticket takes nothing new: no note, no e-mail, no file, no
    /// reassignment.
    /// </summary>
    /// <remarks>
    /// Reopen it first. The closure is what the SLA report reads, and a ticket
    /// that keeps accumulating activity after it closed has a life outside its
    /// own recorded lifetime — two reports of the same month then disagree
    /// depending on when they were run. Reopening is one click and it is
    /// visible in the history, which is the point.
    /// </remarks>
    public static Error? RefuseIfClosed(RequestStatus status, string what)
    {
        ArgumentNullException.ThrowIfNull(status);

        return status.IsClosedState
            ? Error.Conflict(
                "ServiceRequest.Closed",
                $"This ticket is {status.StatusName}. Reopen it before {what}.")
            : null;
    }

    /// <summary>
    /// Stamps the first-response time if nothing has yet.
    /// </summary>
    /// <remarks>
    /// The response SLA asks one question: did anybody get back to them. The
    /// first public note, the first e-mail out and the first status change all
    /// answer it, so all three stamp it — and it is stamped once, because a
    /// "first" that moves is not one.
    /// </remarks>
    public static void StampFirstResponse(ServiceRequest ticket, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        ticket.FirstResponseOnUtc ??= now;
        ticket.ResponseElapsedMinutes ??=
            (int)Math.Max(0, (now - (ticket.SlaStartOnUtc ?? ticket.CreatedOnUtc)).TotalMinutes);
    }
}
