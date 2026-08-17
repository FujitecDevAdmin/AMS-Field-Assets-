namespace AMS.Modules.Organization.Features.UpdateEmployee;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateEmployeeRequest(
    string EmployeeCode,
    string FullName,
    string? Email,
    string? Phone,
    int? DepartmentId,
    int? BranchId,
    int? ReportingManagerId,
    string ETag);
