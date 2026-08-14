using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.SearchRequestQueue;

/// <summary>
/// The technician queue. Catalogue: Service Request Queue.
/// </summary>
/// <remarks>
/// The order is the whole point of the screen: overdue first, then nearest
/// due, then priority, then oldest. IX_ServiceRequest_SlaQueue exists for that
/// ORDER BY, and IsSlaOverdue is a stored column rather than a computed one
/// precisely so an index can carry it — a queue that has to evaluate the SLA
/// for every open ticket before it can sort is a queue that stops sorting once
/// there are enough tickets to need sorting.
/// </remarks>
public sealed class SearchRequestQueueHandler(ServiceDeskDbContext db)
    : IRequestHandler<SearchRequestQueueQuery, SearchRequestQueueResponse>
{
    public async Task<Result<SearchRequestQueueResponse>> HandleAsync(
        SearchRequestQueueQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query =
            from r in db.ServiceRequests
            join s in db.RequestStatuses on r.RequestStatusId equals s.Id
            select new { Request = r, Status = s };

        if (request.OpenOnly)
        {
            query = query.Where(x => !x.Status.IsClosedState);
        }

        if (request.RequestKind is { } kind)
        {
            query = query.Where(x => x.Request.RequestKind == kind);
        }

        if (request.RequestStatusId is { } statusId)
        {
            query = query.Where(x => x.Request.RequestStatusId == statusId);
        }

        if (request.Priority is { } priority)
        {
            query = query.Where(x => x.Request.Priority == priority);
        }

        if (request.AssignedToUserId is { } userId)
        {
            query = query.Where(x => x.Request.AssignedToUserId == userId);
        }

        if (request.AssignedTeamId is { } teamId)
        {
            query = query.Where(x => x.Request.AssignedTeamId == teamId);
        }

        if (request.LocationId is { } locationId)
        {
            query = query.Where(x => x.Request.LocationId == locationId);
        }

        // Nobody holds it. Not "no team" — a ticket sitting with a team is
        // still nobody's, and this filter is what the desk works from first.
        if (request.Unassigned)
        {
            query = query.Where(x => x.Request.AssignedToUserId == null);
        }

        if (request.OverdueOnly)
        {
            query = query.Where(x => x.Request.IsSlaOverdue);
        }

        if (request.Search is { } search)
        {
            query = query.Where(x =>
                x.Request.RequestNumber.Contains(search)
                || x.Request.Subject.Contains(search));
        }

        var total = await query.CountAsync(ct);

        // Counted over the filter and not the page: a screen showing "3 of 50
        // overdue" while the page happens to hold none is a screen nobody acts
        // on.
        var overdue = await query.CountAsync(x => x.Request.IsSlaOverdue, ct);

        var rows = await query
            .OrderByDescending(x => x.Request.IsSlaOverdue)
            // A ticket with no due date sorts after every ticket that has one.
            // SQL Server puts NULL first by default, which would float the
            // tickets with no policy to the top of a queue sorted by urgency.
            .ThenBy(x => x.Request.ResolutionDueOnUtc == null)
            .ThenBy(x => x.Request.ResolutionDueOnUtc)
            .ThenBy(x =>
                x.Request.Priority == RequestPriority.Critical ? 0
                : x.Request.Priority == RequestPriority.High ? 1
                : x.Request.Priority == RequestPriority.Medium ? 2
                : 3)
            .ThenBy(x => x.Request.CreatedOnUtc)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(x => new SearchRequestQueueResponse.Row(
                x.Request.Id,
                x.Request.RequestNumber,
                x.Request.RequestKind,
                x.Request.Subject,
                x.Request.Priority,
                x.Request.RequestStatusId,
                x.Status.StatusName,
                x.Status.IsClosedState,
                db.RequestCategories
                    .Where(c => c.Id == x.Request.RequestCategoryId)
                    .Select(c => c.CategoryName)
                    .FirstOrDefault(),
                x.Request.AssignedToUserId,
                x.Request.AssignedTeamId,
                db.SupportTeams
                    .Where(t => t.Id == x.Request.AssignedTeamId)
                    .Select(t => t.TeamName)
                    .FirstOrDefault(),
                x.Request.LocationId,
                x.Request.RequestedByEmployeeId,
                x.Request.ResponseDueOnUtc,
                x.Request.ResolutionDueOnUtc,
                x.Request.IsSlaOverdue,
                x.Request.IsSlaPaused,
                x.Request.CreatedOnUtc))
            .ToListAsync(ct);

        return new SearchRequestQueueResponse(rows, total, overdue);
    }
}
