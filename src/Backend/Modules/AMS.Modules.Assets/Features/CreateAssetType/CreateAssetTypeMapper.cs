namespace AMS.Modules.Assets.Features.CreateAssetType;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateAssetTypeMapper
{
    public static CreateAssetTypeCommand ToCommand(CreateAssetTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateAssetTypeCommand(
            request.TypeName.Trim(),
            request.ParentAssetTypeId,
            request.IsAllocatable ?? true,
            request.IsPhysical ?? true,
            request.IsBulkDefault ?? false,
            request.TracksHardware ?? false,
            request.TracksSoftware ?? false,
            request.TracksVehicle ?? false,
            request.TracksCalibration ?? false);
    }
}
