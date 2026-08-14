namespace AMS.Modules.Assets.Features.UpdateAssetType;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateAssetTypeRequest(
    string TypeName,
    int? ParentAssetTypeId,
    bool? IsAllocatable,
    bool? IsPhysical,
    bool? IsBulkDefault,
    bool? TracksHardware,
    bool? TracksSoftware,
    bool? TracksVehicle,
    bool? TracksCalibration,
    bool IsActive);
