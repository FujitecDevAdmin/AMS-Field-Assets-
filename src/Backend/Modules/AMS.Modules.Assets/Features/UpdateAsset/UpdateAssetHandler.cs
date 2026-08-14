using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.Modules.Assets.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.UpdateAsset;

/// <summary>Edit an asset already on the register.</summary>
/// <remarks>
/// <c>CurrentEmployeeId</c> is deliberately not editable here. Who holds an
/// asset changes through Allocations — allocate, acknowledge, return — and each
/// of those writes its own timeline entry. A register form that could reassign
/// custody silently would let an asset change hands with no acknowledgement and
/// no record of who agreed to it.
/// </remarks>
public sealed class UpdateAssetHandler(
    AssetsDbContext db,
    IAssetTimeline timeline,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateAssetCommand, UpdateAssetResponse>
{
    public async Task<Result<UpdateAssetResponse>> HandleAsync(
        UpdateAssetCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var asset = await db.Assets.SingleOrDefaultAsync(a => a.Id == request.Id, ct);
        if (asset is null)
        {
            return Error.NotFound("Asset", request.Id);
        }

        if (asset.IsDeleted)
        {
            return Error.Validation(
                "Asset.Deleted", "That asset has been removed from the register.");
        }

        var type = await db.AssetTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == request.AssetTypeId, ct);
        if (type is null)
        {
            return Error.NotFound("AssetType", request.AssetTypeId);
        }

        if (!await db.AssetStatuses.AnyAsync(s => s.Id == request.AssetStatusId, ct))
        {
            return Error.NotFound("AssetStatus", request.AssetStatusId);
        }

        if (request.AssetClassId is { } classId
            && !await db.AssetClasses.AnyAsync(c => c.Id == classId, ct))
        {
            return Error.NotFound("AssetClass", classId);
        }

        // The asset's CURRENT holder, not one from the request: custody is
        // Allocations' to change, and the shape rules still have to see it.
        var shape = AssetShape.Validate(
            request.IsBulk, request.Quantity, request.UnitOfMeasure,
            request.CurrentLocationId, asset.CurrentEmployeeId, type);
        if (shape is not null)
        {
            return shape;
        }

        // A unit asset that already belongs to somebody cannot become a bulk
        // line: the quantity would be held by a person, which AssetHolding has
        // no way to express.
        if (request.IsBulk && !asset.IsBulk && asset.CurrentEmployeeId is not null)
        {
            return Error.Validation(
                "Asset.AllocatedCannotBecomeBulk",
                "That asset is issued to somebody. Take it back before recording it in bulk.");
        }

        var previousStatusId = asset.AssetStatusId;

        asset.AssetNumber = request.AssetNumber;
        asset.AssetName = request.AssetName;
        asset.SerialNumber = request.SerialNumber;
        asset.AssetTypeId = request.AssetTypeId;
        asset.AssetClassId = request.AssetClassId;
        asset.Make = request.Make;
        asset.Model = request.Model;
        asset.AssetStatusId = request.AssetStatusId;
        asset.CurrentLocationId = request.CurrentLocationId;
        asset.DepartmentId = request.DepartmentId;
        asset.CostCenter = request.CostCenter;
        asset.AcquisitionDate = request.AcquisitionDate;
        asset.IsBulk = request.IsBulk;
        asset.Quantity = request.Quantity;
        asset.UnitOfMeasure = request.UnitOfMeasure;
        asset.Remarks = request.Remarks;
        asset.ConcurrencyStamp = Guid.NewGuid();
        asset.ModifiedOnUtc = clock.UtcNow;
        asset.ModifiedBy = currentUser.Username;

        // Only a status change earns a timeline line. Every edit writing one
        // would bury the events that matter under "somebody fixed a typo",
        // and the temporal table already records every column that changed.
        if (previousStatusId != request.AssetStatusId)
        {
            var to = await db.AssetStatuses
                .Where(s => s.Id == request.AssetStatusId)
                .Select(s => s.StatusName)
                .SingleAsync(ct);
            var from = await db.AssetStatuses
                .Where(s => s.Id == previousStatusId)
                .Select(s => s.StatusName)
                .SingleOrDefaultAsync(ct);

            await timeline.AppendAsync(
                new AssetTimelineEntry(
                    asset.Id,
                    "StatusChanged",
                    $"Status changed from {from ?? "unknown"} to {to}.",
                    clock.UtcNow,
                    currentUser.Username,
                    LocationId: request.CurrentLocationId),
                ct);
        }

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

        return new UpdateAssetResponse(asset.Id, asset.AssetNumber, asset.AssetName);
    }
}
