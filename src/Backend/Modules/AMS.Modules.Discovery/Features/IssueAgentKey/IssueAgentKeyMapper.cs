namespace AMS.Modules.Discovery.Features.IssueAgentKey;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class IssueAgentKeyMapper
{
    public static IssueAgentKeyCommand ToCommand(IssueAgentKeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new IssueAgentKeyCommand(
            request.KeyName.Trim());
    }
}
