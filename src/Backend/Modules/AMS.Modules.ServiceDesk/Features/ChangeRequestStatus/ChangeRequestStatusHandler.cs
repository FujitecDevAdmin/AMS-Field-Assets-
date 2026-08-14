using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceLevel.PublicApi.ServiceLevel;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.ChangeRequestStatus;

/// <summary>
/// Move a ticket: start it, hold it, resolve it, close it, reopen it.
/// Catalogue: the status bar on Request Detail.
/// </summary>
/// <remarks>
/// There is no transition table. The statuses are data — a site adds
/// "Awaiting Vendor" without a release — and a matrix of which may follow
/// which would have to be maintained alongside them or silently stop matching.
/// What is enforced instead is the small set of things that are true whatever
/// the statuses are called: a ticket cannot move to where it already is, a
/// finished ticket needs its resolution written down, and the clock is charged
/// on every move.
/// </remarks>
public sealed class ChangeRequestStatusHandler(
    ServiceDeskDbContext db,
    ISlaCalculator sla,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<ChangeRequestStatusCommand, ChangeRequestStatusResponse>
{
    public async Task<Result<ChangeRequestStatusResponse>> HandleAsync(
        ChangeRequestStatusCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ticket = await db.ServiceRequests.SingleOrDefaultAsync(r => r.Id == request.Id, ct);
        if (ticket is null)
        {
            return Error.NotFound("ServiceRequest", request.Id);
        }

        var current = await db.RequestStatuses.SingleAsync(s => s.Id == ticket.RequestStatusId, ct);

        var target = await db.RequestStatuses
            .SingleOrDefaultAsync(s => s.Id == request.RequestStatusId, ct);

        if (target is null)
        {
            return Error.NotFound("RequestStatus", request.RequestStatusId);
        }

        if (!target.IsActive)
        {
            return Error.Validation(
                "RequestStatus.Retired",
                $"{target.StatusName} is no longer in use.");
        }

        if (target.Id == current.Id)
        {
            return Error.Validation(
                "ServiceRequest.SameStatus",
                $"This ticket is already {current.StatusName}.");
        }

        // Resolved, Closed and Rejected all stop the clock, and all three are a
        // statement about what happened. Requiring the sentence is the only
        // thing standing between an SLA report and a column of blanks.
        var resolution = request.Resolution ?? ticket.Resolution;

        if (target.SlaClockBehaviour == SlaClockBehaviour.Stopped
            && string.IsNullOrWhiteSpace(resolution))
        {
            return Error.Validation(
                "ServiceRequest.ResolutionRequired",
                $"Say what was done before moving this ticket to {target.StatusName}.");
        }

        var now = clock.UtcNow;
        var reopening = current.IsClosedState && !target.IsClosedState;

        // Operational minutes, measured by the module that owns the branch's
        // working week. This is what makes "a ticket held over a weekend
        // consumes nothing" true rather than aspirational.
        var minutes = await sla.OperationalMinutesAsync(
            ticket.LocationId, SlaClock.SinceLastCalculated(ticket), now, ticket.SlaPolicyId, ct);

        SlaClock.Charge(ticket, current, target, now, minutes);

        ticket.RequestStatusId = target.Id;
        ticket.Resolution = resolution;
        ticket.ModifiedOnUtc = now;
        ticket.ModifiedBy = currentUser.Username;
        TicketGuards.StampFirstResponse(ticket, now);

        if (target.IsClosedState)
        {
            ticket.ClosedOnUtc = now;
            ticket.ResolvedOnUtc ??= now;
        }
        else if (target.SlaClockBehaviour == SlaClockBehaviour.Stopped)
        {
            ticket.ResolvedOnUtc = now;
        }

        if (reopening)
        {
            // The closure is undone, not kept alongside a ticket that is open
            // again: a row with a ClosedOnUtc and an open status is a row every
            // report has to special-case.
            ticket.ClosedOnUtc = null;
            ticket.ResolvedOnUtc = null;
        }

        db.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = ticket.Id,
            EntryKind = HistoryEntryKind.Transition,
            EntryText = reopening
                ? $"Reopened from {current.StatusName}."
                : $"{current.StatusName} to {target.StatusName}.",
            Body = request.Remarks,
            FromStatusId = current.Id,
            ToStatusId = target.Id,
            OccurredOnUtc = now,
            PerformedBy = currentUser.Username,
        });

        await db.SaveChangesAsync(ct);

        return new ChangeRequestStatusResponse(
            ticket.Id, target.Id, target.StatusName, target.IsClosedState,
            ticket.IsSlaPaused, ticket.ResolutionConsumedMinutes);
    }
}
