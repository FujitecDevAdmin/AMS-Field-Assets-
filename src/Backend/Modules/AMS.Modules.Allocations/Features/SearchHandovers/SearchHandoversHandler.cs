using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.SearchHandovers;

/// <summary>What the branch store is holding. Catalogue screen: Branch Handover.</summary>
public sealed class SearchHandoversHandler(AllocationsDbContext db, ICurrentUser currentUser)
    : IRequestHandler<SearchHandoversQuery, SearchHandoversResponse>
{
    public async Task<Result<SearchHandoversResponse>> HandleAsync(
        SearchHandoversQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.AssetHandovers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(h => h.Status == request.Status);
        }

        if (request.BranchLocationId.HasValue)
        {
            query = query.Where(h => h.BranchLocationId == request.BranchLocationId.Value);
        }

        // A handover always has a branch - it is the store holding it - so
        // there is no unplaced case to allow through here.
        if (!currentUser.HasAllBranches)
        {
            var branches = currentUser.BranchIds;
            query = query.Where(h => branches.Contains(h.BranchLocationId));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(h => h.HandedOverOnUtc)
            .ThenByDescending(h => h.Id)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(h => new SearchHandoversResponse.Row(
                h.Id, h.AllocationId, h.AssetId, h.FromEmployeeId, h.BranchLocationId,
                h.Status, h.ReturnCondition, h.Remarks, h.HandedOverOnUtc,
                db.AssetReturnImages.Count(i => i.HandoverId == h.Id)))
            .ToListAsync(ct);

        return new SearchHandoversResponse(rows, total);
    }
}
