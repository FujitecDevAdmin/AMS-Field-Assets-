namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetDepreciationEntry]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetDepreciationEntry
{
    public long Id { get; set; }

    public int AssetId { get; set; }

    public short FinancialYear { get; set; }

    public decimal OpeningAccumulated { get; set; }

    public decimal Additions { get; set; }

    public decimal ChargedForPeriod { get; set; }

    public decimal ClosingAccumulated { get; set; }

    public decimal NetBookValueAtClose { get; set; }

    public required string SourceSystem { get; set; }

    public DateTime SyncedOnUtc { get; set; }
}
