using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.Modules.Assets.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.RegisterAsset;

/// <summary>Register an asset. Catalogue: Register an asset.</summary>
/// <remarks>
/// Every asset the company owns comes through here — Revision 3 made this the
/// register for furniture, factory equipment, vehicles and instruments as well
/// as IT. What a given type of asset is allowed to do is read from
/// <c>AssetType</c>'s behaviour flags rather than hardcoded here.
/// </remarks>
public sealed class RegisterAssetHandler(
    AssetsDbContext db,
    IAssetTimeline timeline,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<RegisterAssetCommand, RegisterAssetResponse>
{
    public async Task<Result<RegisterAssetResponse>> HandleAsync(
        RegisterAssetCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

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

        var shape = AssetShape.Validate(request.IsBulk, request.Quantity, request.UnitOfMeasure,
                                        request.CurrentLocationId, currentEmployeeId: null, type);
        if (shape is not null)
        {
            return shape;
        }

        var asset = new Asset
        {
            AssetNumber = request.AssetNumber,
            AssetName = request.AssetName,
            SerialNumber = request.SerialNumber,
            AssetTypeId = request.AssetTypeId,
            AssetClassId = request.AssetClassId,
            Make = request.Make,
            Model = request.Model,
            AssetStatusId = request.AssetStatusId,
            CurrentLocationId = request.CurrentLocationId,
            DepartmentId = request.DepartmentId,
            CostCenter = request.CostCenter,
            AcquisitionDate = request.AcquisitionDate,
            IsBulk = request.IsBulk,
            Quantity = request.Quantity,
            UnitOfMeasure = request.UnitOfMeasure,
            Remarks = request.Remarks,
            IsDeleted = false,
            ConcurrencyStamp = Guid.NewGuid(),
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.Assets.Add(asset);

        try
        {
            // The asset first, because the timeline entry needs its id, and then
            // both in one SaveChanges — an asset that exists with no "Registered"
            // line is a register whose history starts in the middle.
            await db.SaveChangesAsync(ct);

            await timeline.AppendAsync(
                new AssetTimelineEntry(
                    asset.Id,
                    "Registered",
                    request.IsBulk
                        ? $"Registered as a bulk line of {request.Quantity:0.###} {request.UnitOfMeasure}."
                        : "Registered on the asset register.",
                    clock.UtcNow,
                    currentUser.Username,
                    LocationId: request.CurrentLocationId),
                ct);

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

        return new RegisterAssetResponse(asset.Id, asset.AssetNumber, asset.AssetName);
    }
}
