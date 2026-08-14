using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.CreateAssetType;

/// <summary>
/// Add an asset type. Catalogue: Say what a type of asset can do.
/// </summary>
public sealed record CreateAssetTypeCommand(
    string TypeName,
    int? ParentAssetTypeId,
    bool IsAllocatable,
    bool IsPhysical,
    bool IsBulkDefault,
    bool TracksHardware,
    bool TracksSoftware,
    bool TracksVehicle,
    bool TracksCalibration) : ICommand<CreateAssetTypeResponse>;
