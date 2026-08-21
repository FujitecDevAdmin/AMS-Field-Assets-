namespace AMS.Modules.Verification.Domain;

/// <summary>
/// Mirrors <c>[Verification].[PhysicalVerificationCycle]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class PhysicalVerificationCycle
{
    public int Id { get; set; }

    public required string CycleName { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int BranchId { get; set; }

    public int TotalAssetCount { get; set; }

    public bool IsActive { get; set; }

    public DateTime? ClosedOnUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
