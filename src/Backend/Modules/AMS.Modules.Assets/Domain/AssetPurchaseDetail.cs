namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetPurchaseDetail]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetPurchaseDetail
{
    public int AssetId { get; set; }

    public int? VendorId { get; set; }

    public string? PurchaseOrderNumber { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public decimal? PurchaseCost { get; set; }

    public DateOnly? WarrantyStartDate { get; set; }

    public DateOnly? WarrantyEndDate { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
