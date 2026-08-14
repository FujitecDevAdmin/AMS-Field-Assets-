namespace AMS.Modules.Allocations.Domain;

/// <summary>
/// Mirrors <c>[Allocations].[AssetAllocationApproval]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetAllocationApproval
{
    public int Id { get; set; }

    public int AssetId { get; set; }

    public int EmployeeId { get; set; }

    public int? LocationId { get; set; }

    public required string Status { get; set; }

    public int RequestedByUserId { get; set; }

    public DateTime RequestedOnUtc { get; set; }

    public int? DecidedByUserId { get; set; }

    public DateTime? DecidedOnUtc { get; set; }

    public string? DecisionRemarks { get; set; }

    public int? AllocationId { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
