namespace AMS.Modules.Organization.Features.CreateDepartment;

/// <summary>
/// The new department.
/// </summary>
/// <param name="Id">The new department.</param>
/// <param name="DepartmentName">As stored, trimmed.</param>
public sealed record CreateDepartmentResponse(
    int Id,
    string DepartmentName);
