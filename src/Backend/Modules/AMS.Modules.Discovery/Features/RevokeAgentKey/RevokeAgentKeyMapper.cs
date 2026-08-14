namespace AMS.Modules.Discovery.Features.RevokeAgentKey;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class RevokeAgentKeyMapper
{
    public static RevokeAgentKeyCommand ToCommand(RevokeAgentKeyRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RevokeAgentKeyCommand(
            id);
    }
}
