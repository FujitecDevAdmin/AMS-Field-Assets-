namespace AMS.Modules.Discovery.Domain;

/// <summary>
/// Mirrors <c>[Discovery].[AssetInstalledSoftware]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetInstalledSoftware
{
    public long Id { get; set; }

    public int AssetId { get; set; }

    public required string SoftwareName { get; set; }

    public string? Version { get; set; }

    public string? Publisher { get; set; }

    public DateTime FirstSeenOnUtc { get; set; }

    public DateTime LastSeenOnUtc { get; set; }

    public bool IsRemoved { get; set; }
}
