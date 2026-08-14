using AMS.Modules.Assets.PublicApi;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Persistence;

/// <summary>
/// Reads the custody facts other modules ask for. See
/// <see cref="IAssetSnapshot"/> for why this exists.
/// </summary>
public sealed class AssetSnapshotReader(AssetsDbContext db) : IAssetSnapshot
{
    public async Task<AssetSnapshot?> GetAsync(int assetId, CancellationToken ct) =>
        await db.Assets
            .AsNoTracking()
            .Where(a => a.Id == assetId && !a.IsDeleted)
            .Select(a => new AssetSnapshot(
                a.Id,
                a.AssetNumber,
                a.CurrentEmployeeId,
                a.CurrentLocationId,
                a.DepartmentId,
                a.CostCenter,
                a.IsBulk))
            .SingleOrDefaultAsync(ct);
}
