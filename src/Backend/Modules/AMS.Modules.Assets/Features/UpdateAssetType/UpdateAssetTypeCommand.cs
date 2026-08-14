using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.UpdateAssetType;

/// <summary>
/// Rename a type, move it in the tree, change what it can do, or retire it.
/// </summary>
public sealed record UpdateAssetTypeCommand(
    int Id,
    string TypeName,
    int? ParentAssetTypeId,
    bool IsAllocatable,
    bool IsPhysical,
    bool IsBulkDefault,
    bool TracksHardware,
    bool TracksSoftware,
    bool TracksVehicle,
    bool TracksCalibration,
    bool IsActive) : ICommand<UpdateAssetTypeResponse>;
