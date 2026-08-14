namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Charges elapsed time to a ticket according to the status it was sitting in.
/// </summary>
/// <remarks>
/// <para>
/// The design script says the *Minutes columns are OPERATIONAL minutes, not
/// wall clock: "a ticket held over a weekend consumes nothing". That is only
/// true if something charges the time as it passes, and the only moments we
/// reliably have are the moments the ticket moves. So every status change
/// closes the books on the interval that just ended before opening the next
/// one.
/// </para>
/// <para>
/// Which bucket the interval lands in is decided by the status it was IN, not
/// the one it is going to. Time spent On Hold is paused time even when the
/// move out of it is what discovers that.
/// </para>
/// <para>
/// How LONG the interval was is not decided here. The caller asks ServiceLevel,
/// because the answer depends on the branch's working week and on whether the
/// ticket's policy respects it — neither of which is ServiceDesk's to know.
/// This class decides which bucket, not how many.
/// </para>
/// </remarks>
public static class SlaClock
{
    /// <summary>
    /// Charges everything since the last calculation, then re-arms the clock.
    /// </summary>
    /// <param name="ticket">The ticket. Its minute columns are updated in place.</param>
    /// <param name="leaving">The status the ticket has been sitting in.</param>
    /// <param name="entering">The status it is moving to.</param>
    /// <param name="now">The moment of the move.</param>
    /// <param name="minutes">
    /// The operational minutes that have passed since the clock was last
    /// calculated, as ServiceLevel measures them. Use
    /// <see cref="SinceLastCalculated"/> to find the span to ask about.
    /// </param>
    public static void Charge(
        ServiceRequest ticket,
        RequestStatus leaving,
        RequestStatus entering,
        DateTime now,
        int minutes)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(leaving);
        ArgumentNullException.ThrowIfNull(entering);

        // A clock that can run backwards is a clock that produces negative
        // minutes, and CK_ServiceRequest_SlaMinutes rejects the row rather than
        // storing them. Clamping here means a corrected system time shows up as
        // a gap in the record, not a failed status change.
        minutes = Math.Max(0, minutes);

        if (minutes > 0)
        {
            switch (leaving.SlaClockBehaviour)
            {
                case SlaClockBehaviour.Running:
                    ticket.ResolutionConsumedMinutes += minutes;
                    break;

                case SlaClockBehaviour.Paused:
                    ticket.SlaPausedMinutes += minutes;
                    break;

                // Stopped: the ticket was Resolved or Closed. Time after that
                // belongs to nobody — charging it would make reopening a ticket
                // retrospectively blow an SLA it met.
                default:
                    break;
            }

            if (leaving.CountsTechnicianTime)
            {
                ticket.TechnicianWorkingMinutes += minutes;
            }
        }

        ticket.SlaLastCalculatedOnUtc = now;
        ticket.IsSlaPaused = entering.SlaClockBehaviour == SlaClockBehaviour.Paused;

        // A stopped clock is never overdue: a ticket resolved yesterday does
        // not become late tonight because its due date passed.
        ticket.IsSlaOverdue =
            entering.SlaClockBehaviour != SlaClockBehaviour.Stopped
            && ticket.ResolutionDueOnUtc is { } due
            && now > due;
    }

    /// <summary>
    /// The start of the span the next charge covers.
    /// </summary>
    /// <remarks>
    /// Falls back through the clock's own history: when it was last calculated,
    /// else when it started, else when the ticket was raised. The last of those
    /// is what a ticket raised before SLA policies existed gets.
    /// </remarks>
    public static DateTime SinceLastCalculated(ServiceRequest ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        return ticket.SlaLastCalculatedOnUtc ?? ticket.SlaStartOnUtc ?? ticket.CreatedOnUtc;
    }
}
