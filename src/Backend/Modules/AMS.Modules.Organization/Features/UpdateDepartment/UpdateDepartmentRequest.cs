namespace AMS.Modules.Organization.Features.UpdateDepartment;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateDepartmentRequest(
    string DepartmentName,
    bool IsActive);
