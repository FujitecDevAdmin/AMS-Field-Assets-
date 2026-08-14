namespace AMS.Modules.Identity.Features.SetRoleCapabilities;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SetRoleCapabilitiesMapper
{
    public static SetRoleCapabilitiesCommand ToCommand(SetRoleCapabilitiesRequest request, int roleId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SetRoleCapabilitiesCommand(
            roleId,
            request.CapabilityNames);
    }
}
