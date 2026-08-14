using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.UpdateEmployee;

/// <summary>
/// Edit an employee. Catalogue: Employee directory, Reporting manager.
/// </summary>
public sealed record UpdateEmployeeCommand(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string? Email,
    string? Phone,
    int? DepartmentId,
    int? LocationId,
    int? ReportingManagerId,
    string ETag) : ICommand<UpdateEmployeeResponse>;
