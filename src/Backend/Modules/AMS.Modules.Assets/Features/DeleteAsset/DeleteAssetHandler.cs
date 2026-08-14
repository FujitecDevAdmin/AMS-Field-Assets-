using AMS.Modules.Assets.Persistence;
using AMS.Modules.Assets.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.DeleteAsset;

/// <summary>
/// Remove an asset from the register. Catalogue: "Marked as deleted, never
/// physically removed, so history keeps its meaning."
/// </summary>
/// <remarks>
/// Soft, always. Allocations, movements, tickets, contracts and verifications
/// all hold this asset's id with no FK to stop a hard delete, so removing the
/// row would leave every one of those reports quietly short of lines rather
/// than visibly broken.
/// </remarks>
public sealed class DeleteAssetHandler(
    AssetsDbContext db,
    IAssetTimeline timeline,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<DeleteAssetCommand, DeleteAssetResponse>
{
    public async Task<Result<DeleteAssetResponse>> HandleAsync(
        DeleteAssetCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var asset = await db.Assets.SingleOrDefaultAsync(a => a.Id == request.Id, ct);
        if (asset is null)
        {
            return Error.NotFound("Asset", request.Id);
        }

        // Deleting twice is not an error worth raising, but it must not write a
        // second timeline line saying it happened again.
        if (asset.IsDeleted)
        {
            return new DeleteAssetResponse(asset.Id, true);
        }

        // An asset somebody is holding is not one to quietly remove: the holder
        // would keep a thing the register says does not exist.
        if (asset.CurrentEmployeeId is not null)
        {
            return Error.Validation(
                "Asset.StillAllocated",
                "That asset is issued to somebody. Take it back before removing it.");
        }

        var heldQuantity = await db.AssetHoldings
            .Where(h => h.AssetId == asset.Id)
            .SumAsync(h => (decimal?)h.OnHandQuantity, ct) ?? 0m;
        if (heldQuantity > 0m)
        {
            return Error.Validation(
                "Asset.StillInStock",
                $"{heldQuantity:0.###} {asset.UnitOfMeasure} is still on hand. "
                + "Issue or dispose of the stock before removing the line.");
        }

        asset.IsDeleted = true;
        asset.ConcurrencyStamp = Guid.NewGuid();
        asset.ModifiedOnUtc = clock.UtcNow;
        asset.ModifiedBy = currentUser.Username;

        await timeline.AppendAsync(
            new AssetTimelineEntry(
                asset.Id,
                "Deleted",
                request.Reason is null
                    ? "Removed from the register."
                    : $"Removed from the register: {request.Reason}",
                clock.UtcNow,
                currentUser.Username,
                LocationId: asset.CurrentLocationId),
            ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        return new DeleteAssetResponse(asset.Id, true);
    }
}
