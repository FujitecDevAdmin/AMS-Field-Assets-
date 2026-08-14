namespace AMS.Modules.Allocations.Domain;

/// <summary>
/// Mirrors <c>[Allocations].[AssetAcknowledgement]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetAcknowledgement
{
    public int Id { get; set; }

    public int AllocationId { get; set; }

    public required string Status { get; set; }

    public string? DocumentPath { get; set; }

    public string? SignatureImagePath { get; set; }

    public DateTime? SignedOnUtc { get; set; }

    public int? ManagerUserId { get; set; }

    public DateTime? ManagerApprovedOnUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
