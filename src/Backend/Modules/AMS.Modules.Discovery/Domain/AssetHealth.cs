namespace AMS.Modules.Discovery.Domain;

/// <summary>
/// Mirrors <c>[Discovery].[AssetHealth]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetHealth
{
    public int AssetId { get; set; }

    public required string Hostname { get; set; }

    public decimal CpuPercent { get; set; }

    public decimal MemoryPercent { get; set; }

    public decimal SystemDrivePercent { get; set; }

    public decimal? BatteryHealthPercent { get; set; }

    public int UptimeHours { get; set; }

    public string? LoggedInUser { get; set; }

    public DateTime LastSeenOnUtc { get; set; }
}
