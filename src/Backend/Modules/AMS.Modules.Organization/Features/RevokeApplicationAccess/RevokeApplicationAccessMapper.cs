namespace AMS.Modules.Organization.Features.RevokeApplicationAccess;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class RevokeApplicationAccessMapper
{
    public static RevokeApplicationAccessCommand ToCommand(RevokeApplicationAccessRequest request, int employeeId, int applicationId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RevokeApplicationAccessCommand(
            employeeId,
            applicationId);
    }
}
