using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.SearchAssetTypes;

/// <summary>The asset type tree. Catalogue screen: Asset Types and Custom Fields.</summary>
/// <remarks>
/// Flat, with the parent id on each row, and not paged: the live register runs
/// on a few hundred technical groups. The client assembles the tree, which is
/// cheaper than a recursive CTE and lets the screen re-parent a node without a
/// round trip.
/// </remarks>
public sealed class SearchAssetTypesHandler(AssetsDbContext db)
    : IRequestHandler<SearchAssetTypesQuery, SearchAssetTypesResponse>
{
    public async Task<Result<SearchAssetTypesResponse>> HandleAsync(
        SearchAssetTypesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.AssetTypes.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == request.IsActive.Value);
        }

        var rows = await query
            .OrderBy(t => t.TypeName)
            .Select(t => new SearchAssetTypesResponse.Row(
                t.Id,
                t.TypeName,
                t.ParentAssetTypeId,
                t.IsAllocatable,
                t.IsPhysical,
                t.IsBulkDefault,
                t.TracksHardware,
                t.TracksSoftware,
                t.TracksVehicle,
                t.TracksCalibration,
                t.IsActive,
                // Deleted assets are excluded: a type showing "3 assets" that an
                // administrator cannot find anywhere reads as a bug.
                db.Assets.Count(a => a.AssetTypeId == t.Id && !a.IsDeleted),
                db.CustomFieldDefinitions.Count(f => f.AssetTypeId == t.Id)))
            .ToListAsync(ct);

        return new SearchAssetTypesResponse(rows);
    }
}
