namespace AMS.Modules.Allocations.Domain;

/// <summary>
/// Mirrors <c>[Allocations].[AssetHandover]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetHandover
{
    public int Id { get; set; }

    public int AllocationId { get; set; }

    public int AssetId { get; set; }

    public int FromEmployeeId { get; set; }

    public int BranchLocationId { get; set; }

    public required string Status { get; set; }

    public required string ReturnCondition { get; set; }

    public required string Remarks { get; set; }

    public DateTime HandedOverOnUtc { get; set; }

    public int ReceivedByUserId { get; set; }

    public int? MovementId { get; set; }

    public DateTime? DispatchedOnUtc { get; set; }

    public bool IsReceivedByHo { get; set; }

    public DateTime? ReceivedAtHoOnUtc { get; set; }

    public int? ReceivedAtHoByUserId { get; set; }

    public string? ReceiptRemarks { get; set; }

    public DateTime? CancelledOnUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
