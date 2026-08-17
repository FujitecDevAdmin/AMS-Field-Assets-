using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.CreateEmployee;

/// <summary>
/// Add somebody to the directory. Catalogue: Employee directory, Reporting manager.
/// </summary>
public sealed record CreateEmployeeCommand(
    string EmployeeCode,
    string FullName,
    string? Email,
    string? Phone,
    int? DepartmentId,
    int? BranchId,
    int? ReportingManagerId) : ICommand<CreateEmployeeResponse>;
