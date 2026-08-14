namespace AMS.Modules.ServiceDesk.Features.SetSupportTeamMembers;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SetSupportTeamMembersMapper
{
    public static SetSupportTeamMembersCommand ToCommand(SetSupportTeamMembersRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SetSupportTeamMembersCommand(
            id,
            request.Members ?? []);
    }
}
