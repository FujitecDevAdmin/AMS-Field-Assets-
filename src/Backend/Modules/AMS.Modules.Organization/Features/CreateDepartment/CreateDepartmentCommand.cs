using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.CreateDepartment;

/// <summary>
/// Add a department. Catalogue screen: Departments.
/// </summary>
public sealed record CreateDepartmentCommand(
    string DepartmentName) : ICommand<CreateDepartmentResponse>;
