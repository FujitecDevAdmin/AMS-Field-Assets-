namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetDisposal]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetDisposal
{
    public int Id { get; set; }

    public int AssetId { get; set; }

    public DateOnly DisposalDate { get; set; }

    public decimal DisposalQuantity { get; set; }

    public decimal? DisposalGrossValue { get; set; }

    public decimal? SaleProceeds { get; set; }

    public required string DisposalReason { get; set; }

    public int? ApprovedByUserId { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
