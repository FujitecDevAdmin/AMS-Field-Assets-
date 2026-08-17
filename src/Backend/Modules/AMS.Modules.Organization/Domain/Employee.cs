namespace AMS.Modules.Organization.Domain;

/// <summary>
/// Mirrors <c>[Organization].[Employee]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
/// <remarks>
/// System-versioned. Prior versions live in <c>[Organization].[EmployeeHistory]</c>,
/// readable with <c>TemporalAsOf</c>. The concurrency token is
/// <c>ConcurrencyStamp</c>, NOT the period columns (R2-22).
/// </remarks>
public sealed class Employee
{
    public int Id { get; set; }

    public required string EmployeeCode { get; set; }

    public required string FullName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public int? DepartmentId { get; set; }

    public int? BranchId { get; set; }

    public int? ReportingManagerId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    public Guid ConcurrencyStamp { get; set; }
}
