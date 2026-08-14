using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.SearchAllocationRequests;

/// <summary>The approval queue. Catalogue screen: Allocation Requests.</summary>
public sealed class SearchAllocationRequestsHandler(AllocationsDbContext db)
    : IRequestHandler<SearchAllocationRequestsQuery, SearchAllocationRequestsResponse>
{
    public async Task<Result<SearchAllocationRequestsResponse>> HandleAsync(
        SearchAllocationRequestsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.AssetAllocationApprovals.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(a => a.Status == request.Status);
        }

        if (request.EmployeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == request.EmployeeId.Value);
        }

        var total = await query.CountAsync(ct);

        // Newest first: the queue is worked from the top, and a request raised
        // this morning matters more than one decided last month.
        var rows = await query
            .OrderByDescending(a => a.RequestedOnUtc)
            .ThenByDescending(a => a.Id)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(a => new SearchAllocationRequestsResponse.Row(
                a.Id, a.AssetId, a.EmployeeId, a.LocationId, a.Status,
                a.RequestedByUserId, a.RequestedOnUtc, a.DecidedByUserId, a.DecidedOnUtc,
                a.DecisionRemarks, a.AllocationId))
            .ToListAsync(ct);

        return new SearchAllocationRequestsResponse(rows, total);
    }
}
