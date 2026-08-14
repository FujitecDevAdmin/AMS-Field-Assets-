namespace AMS.Modules.Identity.Features.UpdateRole;

/// <summary>
/// The updated role.
/// </summary>
/// <param name="Id">The role edited.</param>
/// <param name="RoleName">As stored, trimmed.</param>
/// <param name="IsActive">An inactive role grants nothing, which is how a role is retired without unpicking who holds it.</param>
public sealed record UpdateRoleResponse(
    int Id,
    string RoleName,
    bool IsActive);
