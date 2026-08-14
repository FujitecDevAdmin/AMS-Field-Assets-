namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetHolding]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetHolding
{
    public int Id { get; set; }

    public int AssetId { get; set; }

    public int? LocationId { get; set; }

    public int? CustomerSiteId { get; set; }

    public decimal OnHandQuantity { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
