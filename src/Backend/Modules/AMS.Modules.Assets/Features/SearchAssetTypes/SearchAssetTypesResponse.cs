namespace AMS.Modules.Assets.Features.SearchAssetTypes;

/// <summary>
/// Every type, flat, with its parent id. The client builds the tree.
/// </summary>
/// <param name="Rows">The types, with their behaviour flags.</param>
public sealed record SearchAssetTypesResponse(
    IReadOnlyList<SearchAssetTypesResponse.Row> Rows)
{
    /// <summary>One asset type.</summary>
    /// <param name="Id">The type.</param>
    /// <param name="TypeName">Unique, enforced by UX_AssetType_Name.</param>
    /// <param name="ParentAssetTypeId">Null at the root of the tree.</param>
    /// <param name="IsAllocatable">Whether an asset of this type can be issued to a person.</param>
    /// <param name="IsPhysical">0 for software and licences: no serial, no location, no verification.</param>
    /// <param name="IsBulkDefault">Whether new assets of this type default to a bulk line.</param>
    /// <param name="TracksHardware">Whether the hardware detail record applies.</param>
    /// <param name="TracksSoftware">Whether the software detail record applies.</param>
    /// <param name="TracksVehicle">Whether the vehicle detail record applies.</param>
    /// <param name="TracksCalibration">Whether the instrument calibration record applies.</param>
    /// <param name="IsActive">Retired types stay, because assets still point at them.</param>
    /// <param name="AssetCount">Assets of this type, excluding deleted ones.</param>
    /// <param name="CustomFieldCount">Custom fields defined for it.</param>
    public sealed record Row(
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
        bool IsActive,
        int AssetCount,
        int CustomFieldCount);
}
