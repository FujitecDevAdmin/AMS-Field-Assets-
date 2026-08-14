namespace AMS.Modules.Identity.Features.CreateRole;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateRoleMapper
{
    public static CreateRoleCommand ToCommand(CreateRoleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateRoleCommand(
            request.RoleName.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim());
    }
}
