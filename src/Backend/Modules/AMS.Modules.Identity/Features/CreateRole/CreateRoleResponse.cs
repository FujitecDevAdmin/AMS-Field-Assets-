namespace AMS.Modules.Identity.Features.CreateRole;

/// <summary>
/// The new role.
/// </summary>
/// <param name="Id">The new role.</param>
/// <param name="RoleName">As stored, trimmed.</param>
public sealed record CreateRoleResponse(
    int Id,
    string RoleName);
