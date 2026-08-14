using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.SearchMyRequests;

/// <summary>
/// What I have asked for. Catalogue: My Requests.
/// </summary>
/// <remarks>
/// "Mine" means raised BY me or raised FOR me. A manager who asks the desk to
/// set up a joiner appears as the requester, and the joiner appears as the
/// person it is for; both need to see it, and neither should have to know
/// which column they are in.
/// </remarks>
public sealed class SearchMyRequestsHandler(ServiceDeskDbContext db)
    : IRequestHandler<SearchMyRequestsQuery, SearchMyRequestsResponse>
{
    public async Task<Result<SearchMyRequestsResponse>> HandleAsync(
        SearchMyRequestsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // A user account with no employee record behind it — a service account,
        // or one whose linkage was never made. It has no requests of its own,
        // and returning the whole table would be worse than saying so.
        if (request.EmployeeId <= 0)
        {
            return Error.Validation(
                "ServiceRequest.NoEmployee",
                "Your account is not linked to an employee record, so it has no requests.");
        }

        var query =
            from r in db.ServiceRequests
            join s in db.RequestStatuses on r.RequestStatusId equals s.Id
            where r.RequestedByEmployeeId == request.EmployeeId
                || r.OnBehalfOfEmployeeId == request.EmployeeId
            select new { Request = r, Status = s };

        if (request.OpenOnly)
        {
            query = query.Where(x => !x.Status.IsClosedState);
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(x => x.Request.CreatedOnUtc)
            .ThenByDescending(x => x.Request.Id)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(x => new SearchMyRequestsResponse.Row(
                x.Request.Id,
                x.Request.RequestNumber,
                x.Request.RequestKind,
                x.Request.Subject,
                x.Request.Priority,
                x.Status.StatusName,
                x.Status.IsClosedState,
                x.Request.ResolutionDueOnUtc,
                x.Request.IsSlaOverdue,
                x.Request.CreatedOnUtc,
                x.Request.ClosedOnUtc))
            .ToListAsync(ct);

        return new SearchMyRequestsResponse(rows, total);
    }
}
