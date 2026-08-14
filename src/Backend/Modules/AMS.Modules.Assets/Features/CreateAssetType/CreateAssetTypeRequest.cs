namespace AMS.Modules.Assets.Features.CreateAssetType;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateAssetTypeRequest(
    string TypeName,
    int? ParentAssetTypeId,
    bool? IsAllocatable,
    bool? IsPhysical,
    bool? IsBulkDefault,
    bool? TracksHardware,
    bool? TracksSoftware,
    bool? TracksVehicle,
    bool? TracksCalibration);
