using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.SearchAssetStatuses;

/// <summary>The asset status lookup. Catalogue screen: Asset Statuses.</summary>
public sealed class SearchAssetStatusesHandler(AssetsDbContext db)
    : IRequestHandler<SearchAssetStatusesQuery, SearchAssetStatusesResponse>
{
    public async Task<Result<SearchAssetStatusesResponse>> HandleAsync(
        SearchAssetStatusesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.AssetStatuses.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == request.IsActive.Value);
        }

        var rows = await query
            // DisplayOrder then name: the seeded orders leave gaps on purpose so
            // an administrator can slot a status in, and two rows sharing an
            // order must still come back in a stable sequence.
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.StatusName)
            .Select(s => new SearchAssetStatusesResponse.Row(
                s.Id,
                s.StatusName,
                s.IsTerminal,
                s.DisplayOrder,
                s.IsActive,
                db.Assets.Count(a => a.AssetStatusId == s.Id && !a.IsDeleted)))
            .ToListAsync(ct);

        return new SearchAssetStatusesResponse(rows);
    }
}
