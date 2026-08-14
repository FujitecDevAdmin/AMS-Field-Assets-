using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.UpdateAssetType;

/// <summary>
/// Rename a type, move it in the tree, change what it can do, or retire it.
/// </summary>
public sealed class UpdateAssetTypeHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateAssetTypeCommand, UpdateAssetTypeResponse>
{
    public async Task<Result<UpdateAssetTypeResponse>> HandleAsync(
        UpdateAssetTypeCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var type = await db.AssetTypes.SingleOrDefaultAsync(t => t.Id == request.Id, ct);
        if (type is null)
        {
            return Error.NotFound("AssetType", request.Id);
        }

        if (request.ParentAssetTypeId is { } parentId)
        {
            // A type that is its own parent disappears from the tree the client
            // builds - it is never reached from a root - so the screen simply
            // loses it. The database has no opinion: the self-FK is satisfied.
            if (parentId == request.Id)
            {
                return Error.Validation(
                    "AssetType.ParentIsSelf", "An asset type cannot be its own parent.");
            }

            if (!await db.AssetTypes.AnyAsync(t => t.Id == parentId, ct))
            {
                return Error.NotFound("AssetType", parentId);
            }

            if (await WouldMakeACycleAsync(request.Id, parentId, ct))
            {
                return Error.Validation(
                    "AssetType.ParentIsDescendant",
                    "That parent sits under this type, so the move would make a loop.");
            }
        }

        type.TypeName = request.TypeName;
        type.ParentAssetTypeId = request.ParentAssetTypeId;
        type.IsAllocatable = request.IsAllocatable;
        type.IsPhysical = request.IsPhysical;
        type.IsBulkDefault = request.IsBulkDefault;
        type.TracksHardware = request.TracksHardware;
        type.TracksSoftware = request.TracksSoftware;
        type.TracksVehicle = request.TracksVehicle;
        type.TracksCalibration = request.TracksCalibration;
        type.IsActive = request.IsActive;
        type.ModifiedOnUtc = clock.UtcNow;
        type.ModifiedBy = currentUser.Username;

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

        return new UpdateAssetTypeResponse(type.Id, type.TypeName, type.IsActive);
    }

    /// <summary>
    /// Whether re-parenting <paramref name="typeId"/> under
    /// <paramref name="newParentId"/> would close a loop.
    /// </summary>
    /// <remarks>
    /// Walks up from the proposed parent looking for the type being moved. A
    /// self-referencing FK cannot express acyclicity, and a loop here is not a
    /// harmless oddity: every screen that renders the tree recurses, so
    /// A → B → A is a hang rather than a bad-looking list.
    ///
    /// The walk is bounded by the number of types, so a loop already in the
    /// data cannot make this method the thing that hangs.
    /// </remarks>
    private async Task<bool> WouldMakeACycleAsync(int typeId, int newParentId, CancellationToken ct)
    {
        var parents = await db.AssetTypes
            .AsNoTracking()
            .Select(t => new { t.Id, t.ParentAssetTypeId })
            .ToDictionaryAsync(t => t.Id, t => t.ParentAssetTypeId, ct);

        var seen = new HashSet<int>();
        int? cursor = newParentId;
        while (cursor is { } id && seen.Add(id))
        {
            if (id == typeId)
            {
                return true;
            }

            cursor = parents.TryGetValue(id, out var parent) ? parent : null;
        }

        return false;
    }
}
