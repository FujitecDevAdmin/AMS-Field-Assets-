namespace AMS.Modules.Organization.Features.GetEmployee;

/// <summary>
/// Everything the Employee Directory form shows for one person.
/// </summary>
/// <param name="Id">See the handler.</param>
/// <param name="EmployeeCode">See the handler.</param>
/// <param name="FullName">See the handler.</param>
/// <param name="Email">See the handler.</param>
/// <param name="Phone">See the handler.</param>
/// <param name="DepartmentId">See the handler.</param>
/// <param name="DepartmentName">Denormalised for display; null when DepartmentId is.</param>
/// <param name="BranchId">See the handler.</param>
/// <param name="BranchName">Denormalised for display; null when BranchId is.</param>
/// <param name="ReportingManagerId">See the handler.</param>
/// <param name="ReportingManagerName">Denormalised for display; null when the employee reports to nobody.</param>
/// <param name="IsActive">See the handler.</param>
/// <param name="ETag">The ConcurrencyStamp. Employee is system-versioned, so the token is ConcurrencyStamp and NOT a rowversion (R2-22). A mismatch is a 412.</param>
public sealed record GetEmployeeResponse(
    int Id,
    string EmployeeCode,
    string FullName,
    string? Email,
    string? Phone,
    int? DepartmentId,
    string? DepartmentName,
    int? BranchId,
    string? BranchName,
    int? ReportingManagerId,
    string? ReportingManagerName,
    bool IsActive,
    string ETag);
