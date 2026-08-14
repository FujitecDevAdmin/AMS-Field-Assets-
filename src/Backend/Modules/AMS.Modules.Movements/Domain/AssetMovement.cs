namespace AMS.Modules.Movements.Domain;

/// <summary>
/// Mirrors <c>[Movements].[AssetMovement]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetMovement
{
    public int Id { get; set; }

    public int AssetId { get; set; }

    public int? MovementBatchId { get; set; }

    public int? HandoverId { get; set; }

    /// <summary>Defaults to <c>1</c>, as <c>DF_AssetMovement_Quantity</c> does.</summary>
    public decimal Quantity { get; set; } = 1m;

    public required string MovementType { get; set; }

    public int FromLocationId { get; set; }

    public int ToLocationId { get; set; }

    public required string Status { get; set; }

    public string? CourierName { get; set; }

    public string? TrackingNumber { get; set; }

    public string? ChallanNumber { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateOnly? InvoiceDate { get; set; }

    public string? DocumentPath { get; set; }

    public DateTime ShippedOnUtc { get; set; }

    public DateTime? ReceivedOnUtc { get; set; }

    public int? ReceivedByUserId { get; set; }

    public string? ReceiptRemarks { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
