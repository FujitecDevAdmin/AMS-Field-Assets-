using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.SearchAllocations;

/// <summary>
/// Live allocations, expected returns and the overdue list. Catalogue screen:
/// Allocations.
/// </summary>
/// <remarks>
/// Overdue is computed on read, not stored. A stored flag is wrong every night
/// between midnight and whenever a job gets round to fixing it, and the list it
/// drives is the one somebody chases people from.
/// </remarks>
public sealed class SearchAllocationsHandler(
    AllocationsDbContext db,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<SearchAllocationsQuery, SearchAllocationsResponse>
{
    public async Task<Result<SearchAllocationsResponse>> HandleAsync(
        SearchAllocationsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var today = DateOnly.FromDateTime(clock.UtcNow);
        var query = db.AssetAllocations.AsNoTracking();

        if (request.OpenOnly)
        {
            query = query.Where(a => a.ReturnedOnUtc == null);
        }

        // Branch scoping, per request, inside the query handler - where
        // ICurrentUser says it belongs. An allocation with no branch is
        // included: it belongs to nobody in particular and hiding it would
        // lose it entirely.
        if (!currentUser.HasAllBranches)
        {
            var branches = currentUser.BranchIds;
            query = query.Where(a => a.LocationId == null || branches.Contains(a.LocationId.Value));
        }

        if (request.AssetId.HasValue)
        {
            query = query.Where(a => a.AssetId == request.AssetId.Value);
        }

        if (request.EmployeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == request.EmployeeId.Value);
        }

        if (request.LocationId.HasValue)
        {
            query = query.Where(a => a.LocationId == request.LocationId.Value);
        }

        if (request.OverdueOnly)
        {
            query = query.Where(a => a.ReturnedOnUtc == null
                                  && a.ExpectedReturnDate != null
                                  && a.ExpectedReturnDate < today);
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(a => a.AllocatedOnUtc)
            .ThenByDescending(a => a.Id)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(a => new SearchAllocationsResponse.Row(
                a.Id, a.AssetId, a.EmployeeId, a.LocationId, a.AllocatedOnUtc,
                a.ExpectedReturnDate, a.ReturnRequestedOnUtc, a.ReturnedOnUtc,
                a.ReturnedOnUtc == null && a.ExpectedReturnDate != null && a.ExpectedReturnDate < today,
                db.AssetAcknowledgements.Where(k => k.AllocationId == a.Id)
                    .Select(k => k.Status).FirstOrDefault()))
            .ToListAsync(ct);

        return new SearchAllocationsResponse(rows, total);
    }
}
