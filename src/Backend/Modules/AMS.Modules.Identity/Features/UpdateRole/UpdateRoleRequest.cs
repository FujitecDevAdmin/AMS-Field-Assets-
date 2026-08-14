namespace AMS.Modules.Identity.Features.UpdateRole;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateRoleRequest(
    string RoleName,
    string? Description,
    bool IsActive);
