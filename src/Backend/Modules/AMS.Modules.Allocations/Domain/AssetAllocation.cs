namespace AMS.Modules.Allocations.Domain;

/// <summary>
/// Mirrors <c>[Allocations].[AssetAllocation]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetAllocation
{
    public int Id { get; set; }

    public int AssetId { get; set; }

    public int EmployeeId { get; set; }

    public int? LocationId { get; set; }

    public DateTime AllocatedOnUtc { get; set; }

    public DateOnly? ExpectedReturnDate { get; set; }

    public DateTime? ReturnRequestedOnUtc { get; set; }

    public DateTime? ReturnedOnUtc { get; set; }

    public int? ReceivedByUserId { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
