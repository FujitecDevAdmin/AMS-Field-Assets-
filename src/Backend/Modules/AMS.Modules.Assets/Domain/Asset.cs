namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[Asset]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
/// <remarks>
/// System-versioned. Prior versions live in <c>[Assets].[AssetHistory]</c>,
/// readable with <c>TemporalAsOf</c>. The concurrency token is
/// <c>ConcurrencyStamp</c>, NOT the period columns (R2-22).
/// </remarks>
public sealed class Asset
{
    public int Id { get; set; }

    public required string AssetNumber { get; set; }

    public required string AssetName { get; set; }

    public string? SerialNumber { get; set; }

    public int AssetTypeId { get; set; }

    public int? AssetClassId { get; set; }

    public string? Make { get; set; }

    public string? Model { get; set; }

    public int AssetStatusId { get; set; }

    public int? CurrentLocationId { get; set; }

    public int? CurrentEmployeeId { get; set; }

    public int? DepartmentId { get; set; }

    public string? CostCenter { get; set; }

    public DateOnly? AcquisitionDate { get; set; }

    public string? QrCodeValue { get; set; }

    public string? BarcodeValue { get; set; }

    public string? ErpAssetNumber { get; set; }

    public string? SapAssetNumber { get; set; }

    public string? SapAssetClass { get; set; }

    public string? SapPlant { get; set; }

    public DateTime? LastSapSyncOnUtc { get; set; }

    public DateTime? LastPhysicalCheckOnUtc { get; set; }

    public string? Remarks { get; set; }

    /// <summary>The original 70-column FAR row, retained for import-detail display.</summary>
    public string? ImportedDataJson { get; set; }

    public bool IsBulk { get; set; }

    /// <summary>Defaults to <c>1</c>, as <c>DF_Asset_Quantity</c> does.</summary>
    public decimal Quantity { get; set; } = 1m;

    public string? UnitOfMeasure { get; set; }

    public int? CapitalisedFromAssetId { get; set; }

    public int? SplitFromAssetId { get; set; }

    public int? ImportBatchId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    public Guid ConcurrencyStamp { get; set; }
}
