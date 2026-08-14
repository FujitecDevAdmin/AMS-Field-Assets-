namespace AMS.Modules.Organization.Features.GrantApplicationAccess;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GrantApplicationAccessMapper
{
    public static GrantApplicationAccessCommand ToCommand(GrantApplicationAccessRequest request, int employeeId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GrantApplicationAccessCommand(
            employeeId,
            request.ApplicationId,
            string.IsNullOrWhiteSpace(request.ApplicationLoginId) ? null : request.ApplicationLoginId.Trim());
    }
}
