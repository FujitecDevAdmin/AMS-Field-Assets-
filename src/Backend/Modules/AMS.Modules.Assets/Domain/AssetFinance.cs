namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetFinance]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetFinance
{
    public int AssetId { get; set; }

    public decimal? OriginalValue { get; set; }

    public decimal? MigratedBookValue { get; set; }

    public decimal? AdditionalValue { get; set; }

    public decimal? GrossValue { get; set; }

    public decimal? DisposalGrossValue { get; set; }

    public decimal? AccumulatedDepreciation { get; set; }

    public decimal? NetBookValue { get; set; }

    public string? DepreciationMethod { get; set; }

    public decimal? DepreciationPercent { get; set; }

    public int? UsefulLifeMonths { get; set; }

    public decimal? CapitalisedQuantity { get; set; }

    public DateOnly? FirstAcquisitionDate { get; set; }

    public DateOnly? PostingDate { get; set; }

    public string? SapPostingStatus { get; set; }

    public string? AucReference { get; set; }

    public string? OpportunityName { get; set; }

    public string? VoucherNo { get; set; }

    public string? ApVoucherNo { get; set; }

    public int? GrossValueCoaId { get; set; }

    public int? AccumulatedDepreciationCoaId { get; set; }

    public int? DepreciationChargeCoaId { get; set; }

    public DateTime? LastSyncedOnUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
