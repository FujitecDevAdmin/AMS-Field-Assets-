namespace AMS.Modules.Identity.Features.AssignUserRoles;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class AssignUserRolesMapper
{
    public static AssignUserRolesCommand ToCommand(AssignUserRolesRequest request, int userId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AssignUserRolesCommand(
            userId,
            request.RoleIds);
    }
}
