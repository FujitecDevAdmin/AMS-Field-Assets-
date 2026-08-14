namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetHardwareDetail]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetHardwareDetail
{
    public int AssetId { get; set; }

    public string? Hostname { get; set; }

    public string? ChassisType { get; set; }

    public string? Processor { get; set; }

    public int? MemoryGb { get; set; }

    public int? StorageGb { get; set; }

    public string? MonitorModel { get; set; }

    public string? MonitorSerialNumber { get; set; }

    public string? MacAddress { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
