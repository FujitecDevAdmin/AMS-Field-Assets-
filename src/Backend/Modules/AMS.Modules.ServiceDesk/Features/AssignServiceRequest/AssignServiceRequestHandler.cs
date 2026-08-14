using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceLevel.PublicApi.ServiceLevel;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.AssignServiceRequest;

/// <summary>
/// Hand a ticket to somebody. Catalogue: Assign, on the queue and on Request
/// Detail.
/// </summary>
/// <remarks>
/// A ticket may sit with a team, with a person, or with a person inside a team.
/// What it may not do is sit with nobody once somebody has picked it up, so
/// this slice takes at least one of the two and never clears both.
/// </remarks>
public sealed class AssignServiceRequestHandler(
    ServiceDeskDbContext db,
    ISlaCalculator sla,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<AssignServiceRequestCommand, AssignServiceRequestResponse>
{
    public async Task<Result<AssignServiceRequestResponse>> HandleAsync(
        AssignServiceRequestCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.AssignedToUserId is null && request.AssignedTeamId is null)
        {
            return Error.Validation(
                "ServiceRequest.AssigneeRequired",
                "Name a technician, a team, or both.");
        }

        var ticket = await db.ServiceRequests.SingleOrDefaultAsync(r => r.Id == request.Id, ct);
        if (ticket is null)
        {
            return Error.NotFound("ServiceRequest", request.Id);
        }

        var current = await db.RequestStatuses.SingleAsync(s => s.Id == ticket.RequestStatusId, ct);

        var closed = TicketGuards.RefuseIfClosed(current, "reassigning it");
        if (closed is not null)
        {
            return closed;
        }

        if (request.AssignedTeamId is { } teamId)
        {
            var team = await db.SupportTeams.SingleOrDefaultAsync(t => t.Id == teamId, ct);
            if (team is null)
            {
                return Error.NotFound("SupportTeam", teamId);
            }

            if (!team.IsActive)
            {
                return Error.Validation(
                    "SupportTeam.Retired",
                    "That team has been retired. Choose another.");
            }
        }

        var now = clock.UtcNow;

        ticket.AssignedToUserId = request.AssignedToUserId;
        ticket.AssignedTeamId = request.AssignedTeamId ?? ticket.AssignedTeamId;
        ticket.AssignedOnUtc = now;
        ticket.ModifiedOnUtc = now;
        ticket.ModifiedBy = currentUser.Username;

        var status = current;

        // Assigning a brand-new ticket moves it on, because a ticket somebody
        // holds is not still waiting to be looked at. The next status is found
        // by display order rather than by the name 'Assigned': the status list
        // is data, and a site that renames it should not lose the move.
        if (request.AssignedToUserId is not null && await IsFirstOpenStatusAsync(current, ct))
        {
            var next = await db.RequestStatuses
                .Where(s => s.IsActive && !s.IsClosedState && s.DisplayOrder > current.DisplayOrder)
                .OrderBy(s => s.DisplayOrder)
                .FirstOrDefaultAsync(ct);

            if (next is not null)
            {
                var minutes = await sla.OperationalMinutesAsync(
                    ticket.LocationId, SlaClock.SinceLastCalculated(ticket), now,
                    ticket.SlaPolicyId, ct);

                SlaClock.Charge(ticket, current, next, now, minutes);
                ticket.RequestStatusId = next.Id;
                status = next;

                db.RequestHistories.Add(new RequestHistory
                {
                    ServiceRequestId = ticket.Id,
                    EntryKind = HistoryEntryKind.Transition,
                    EntryText = $"{current.StatusName} to {next.StatusName}.",
                    FromStatusId = current.Id,
                    ToStatusId = next.Id,
                    OccurredOnUtc = now,
                    PerformedBy = currentUser.Username,
                });
            }
        }

        db.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = ticket.Id,
            EntryKind = HistoryEntryKind.Transition,
            EntryText = Describe(request),
            Body = request.Remarks,
            AssignedToUserId = request.AssignedToUserId,
            OccurredOnUtc = now,
            PerformedBy = currentUser.Username,
        });

        await db.SaveChangesAsync(ct);

        return new AssignServiceRequestResponse(
            ticket.Id, ticket.AssignedToUserId, ticket.AssignedTeamId, status.Id, status.StatusName);
    }

    private async Task<bool> IsFirstOpenStatusAsync(RequestStatus current, CancellationToken ct) =>
        !await db.RequestStatuses.AnyAsync(
            s => s.IsActive && !s.IsClosedState && s.DisplayOrder < current.DisplayOrder, ct);

    private static string Describe(AssignServiceRequestCommand request) =>
        request.AssignedToUserId is { } user
            ? $"Assigned to user {user}."
            : $"Assigned to team {request.AssignedTeamId}.";
}
