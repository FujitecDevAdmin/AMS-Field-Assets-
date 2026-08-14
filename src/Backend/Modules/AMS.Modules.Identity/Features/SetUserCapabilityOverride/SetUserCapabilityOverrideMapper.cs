namespace AMS.Modules.Identity.Features.SetUserCapabilityOverride;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SetUserCapabilityOverrideMapper
{
    public static SetUserCapabilityOverrideCommand ToCommand(SetUserCapabilityOverrideRequest request, int userId, string capabilityName)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SetUserCapabilityOverrideCommand(
            userId,
            capabilityName,
            request.IsGranted,
            request.Reason?.Trim());
    }
}
