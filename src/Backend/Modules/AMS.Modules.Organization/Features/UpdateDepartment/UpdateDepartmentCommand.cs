using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.UpdateDepartment;

/// <summary>
/// Rename a department or retire it. Catalogue screen: Departments.
/// </summary>
public sealed record UpdateDepartmentCommand(
    int Id,
    string DepartmentName,
    bool IsActive) : ICommand<UpdateDepartmentResponse>;
