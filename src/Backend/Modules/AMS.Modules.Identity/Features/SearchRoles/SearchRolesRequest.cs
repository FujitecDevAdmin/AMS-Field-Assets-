namespace AMS.Modules.Identity.Features.SearchRoles;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchRolesRequest(
    bool? IsActive);
