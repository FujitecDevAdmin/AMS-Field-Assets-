namespace AMS.Modules.Organization.Features.CreateEmployee;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateEmployeeRequest(
    string EmployeeCode,
    string FullName,
    string? Email,
    string? Phone,
    int? DepartmentId,
    int? BranchId,
    int? ReportingManagerId);
