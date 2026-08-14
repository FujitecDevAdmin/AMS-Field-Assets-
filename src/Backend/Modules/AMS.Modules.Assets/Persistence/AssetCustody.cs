using AMS.Modules.Assets.PublicApi;
using AMS.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Persistence;

/// <summary>
/// Moves an asset's branch on behalf of whichever module received it. See
/// <see cref="IAssetCustody"/> for why this exists.
/// </summary>
public sealed class AssetCustody(AssetsDbContext db, IClock clock, ICurrentUser currentUser)
    : IAssetCustody
{
    public async Task<bool> ReceiveAtLocationAsync(
        int assetId,
        int locationId,
        CancellationToken ct)
    {
        var asset = await db.Assets.SingleOrDefaultAsync(a => a.Id == assetId && !a.IsDeleted, ct);
        if (asset is null)
        {
            return false;
        }

        asset.CurrentLocationId = locationId;

        // R2-22: the concurrency token on a temporal table is this, not
        // SysStartTime. Rolling it means a stale editor of the same asset gets
        // a 412 rather than silently overwriting the arrival.
        asset.ConcurrencyStamp = Guid.NewGuid();
        asset.ModifiedOnUtc = clock.UtcNow;
        asset.ModifiedBy = currentUser.Username;

        // Saves its own context, for the same reason IAssetTimeline does: the
        // calling module saves a DIFFERENT context, so leaving it staged would
        // drop the move silently and the asset would stay at the branch it left.
        // Saving is not committing — the dispatcher owns the transaction.
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ApplyTransferAsync(
        int assetId,
        int? employeeId,
        int? departmentId,
        int? locationId,
        string? costCenter,
        CancellationToken ct)
    {
        var asset = await db.Assets.SingleOrDefaultAsync(a => a.Id == assetId && !a.IsDeleted, ct);
        if (asset is null)
        {
            return false;
        }

        // Only what was supplied. A null is "leave it alone" - a cost-centre
        // transfer must not silently unassign whoever is holding the thing.
        if (employeeId is not null)
        {
            asset.CurrentEmployeeId = employeeId;
        }

        if (departmentId is not null)
        {
            asset.DepartmentId = departmentId;
        }

        if (locationId is not null)
        {
            asset.CurrentLocationId = locationId;
        }

        if (costCenter is not null)
        {
            asset.CostCenter = costCenter;
        }

        asset.ConcurrencyStamp = Guid.NewGuid();
        asset.ModifiedOnUtc = clock.UtcNow;
        asset.ModifiedBy = currentUser.Username;

        await db.SaveChangesAsync(ct);
        return true;
    }
}
