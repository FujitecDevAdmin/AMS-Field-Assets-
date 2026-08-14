namespace AMS.Modules.Movements.Domain;

/// <summary>
/// Mirrors <c>[Movements].[MovementBatch]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class MovementBatch
{
    public int Id { get; set; }

    public required string BatchNumber { get; set; }

    public int FromLocationId { get; set; }

    public int ToLocationId { get; set; }

    public required string MovementType { get; set; }

    public required string InvoiceNumber { get; set; }

    public DateOnly InvoiceDate { get; set; }

    public required string CourierName { get; set; }

    public string? TrackingNumber { get; set; }

    public string? ChallanNumber { get; set; }

    public string? DocumentPath { get; set; }

    public required string Remarks { get; set; }

    public int ItemCount { get; set; }

    public int DispatchedByUserId { get; set; }

    public DateTime ShippedOnUtc { get; set; }

    public DateTime? ReceivedOnUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
