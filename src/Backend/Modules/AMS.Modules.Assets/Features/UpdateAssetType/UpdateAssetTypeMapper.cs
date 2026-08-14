namespace AMS.Modules.Assets.Features.UpdateAssetType;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateAssetTypeMapper
{
    public static UpdateAssetTypeCommand ToCommand(UpdateAssetTypeRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateAssetTypeCommand(
            id,
            request.TypeName.Trim(),
            request.ParentAssetTypeId,
            request.IsAllocatable ?? true,
            request.IsPhysical ?? true,
            request.IsBulkDefault ?? false,
            request.TracksHardware ?? false,
            request.TracksSoftware ?? false,
            request.TracksVehicle ?? false,
            request.TracksCalibration ?? false,
            request.IsActive);
    }
}
