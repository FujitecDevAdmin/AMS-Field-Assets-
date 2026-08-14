namespace AMS.Modules.Identity.Features.UpdateRole;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateRoleMapper
{
    public static UpdateRoleCommand ToCommand(UpdateRoleRequest request, int roleId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateRoleCommand(
            roleId,
            request.RoleName.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            request.IsActive);
    }
}
