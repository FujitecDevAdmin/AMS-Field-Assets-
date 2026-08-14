namespace AMS.Modules.Identity.Features.AssignUserRoles;

/// <summary>
/// The roles the user now holds.
/// </summary>
/// <param name="UserId">The user changed.</param>
/// <param name="RoleIds">The complete set afterwards, not a delta.</param>
public sealed record AssignUserRolesResponse(
    int UserId,
    IReadOnlyList<int> RoleIds);
