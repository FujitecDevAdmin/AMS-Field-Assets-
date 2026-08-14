namespace AMS.Modules.SapSync.Domain;

/// <summary>
/// Mirrors <c>[SapSync].[SapSyncWatermark]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class SapSyncWatermark
{
    public int Id { get; set; }

    public required string SyncType { get; set; }

    public DateTime LastChangedOnUtc { get; set; }

    public DateTime UpdatedOnUtc { get; set; }
}
