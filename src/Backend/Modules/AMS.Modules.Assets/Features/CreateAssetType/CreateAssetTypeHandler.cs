using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.CreateAssetType;

/// <summary>Add an asset type. Catalogue: Say what a type of asset can do.</summary>
/// <remarks>
/// The seven flags are the point of the screen. What an asset type CAN DO is
/// data, so adding "Barricade" or "Torque Wrench" is an administrator's job
/// rather than a release.
/// </remarks>
public sealed class CreateAssetTypeHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CreateAssetTypeCommand, CreateAssetTypeResponse>
{
    public async Task<Result<CreateAssetTypeResponse>> HandleAsync(
        CreateAssetTypeCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The parent is checked because there is no FK violation to catch: the
        // self-referencing FK would fire, but as a 547 the translator treats as
        // a coding bug. A bad parent id is a user mistake and deserves a 404.
        if (request.ParentAssetTypeId is { } parentId
            && !await db.AssetTypes.AnyAsync(t => t.Id == parentId, ct))
        {
            return Error.NotFound("AssetType", parentId);
        }

        var type = new AssetType
        {
            TypeName = request.TypeName,
            ParentAssetTypeId = request.ParentAssetTypeId,
            IsAllocatable = request.IsAllocatable,
            IsPhysical = request.IsPhysical,
            IsBulkDefault = request.IsBulkDefault,
            TracksHardware = request.TracksHardware,
            TracksSoftware = request.TracksSoftware,
            TracksVehicle = request.TracksVehicle,
            TracksCalibration = request.TracksCalibration,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.AssetTypes.Add(type);

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

        return new CreateAssetTypeResponse(type.Id, type.TypeName);
    }
}
