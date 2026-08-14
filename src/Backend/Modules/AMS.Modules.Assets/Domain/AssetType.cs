namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetType]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetType
{
    public int Id { get; set; }

    public required string TypeName { get; set; }

    public int? ParentAssetTypeId { get; set; }

    /// <summary>Defaults to <c>1</c>, as <c>DF_AssetType_IsAllocatable</c> does.</summary>
    public bool IsAllocatable { get; set; } = true;

    /// <summary>Defaults to <c>1</c>, as <c>DF_AssetType_IsPhysical</c> does.</summary>
    public bool IsPhysical { get; set; } = true;

    public bool IsBulkDefault { get; set; }

    public bool TracksHardware { get; set; }

    public bool TracksSoftware { get; set; }

    public bool TracksVehicle { get; set; }

    public bool TracksCalibration { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
