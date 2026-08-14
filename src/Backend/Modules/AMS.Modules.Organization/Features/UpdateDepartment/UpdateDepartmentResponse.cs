namespace AMS.Modules.Organization.Features.UpdateDepartment;

/// <summary>
/// The updated department.
/// </summary>
/// <param name="Id">The department edited.</param>
/// <param name="DepartmentName">As stored, trimmed.</param>
/// <param name="IsActive">Retiring is deactivation, never deletion: rows elsewhere still point at this one.</param>
public sealed record UpdateDepartmentResponse(
    int Id,
    string DepartmentName,
    bool IsActive);
