namespace AMS.Modules.Discovery.Domain;

/// <summary>
/// Mirrors <c>[Discovery].[AssetHealthHistory]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetHealthHistory
{
    public long Id { get; set; }

    public int AssetId { get; set; }

    public decimal CpuPercent { get; set; }

    public decimal MemoryPercent { get; set; }

    public decimal SystemDrivePercent { get; set; }

    public DateTime CapturedOnUtc { get; set; }
}
