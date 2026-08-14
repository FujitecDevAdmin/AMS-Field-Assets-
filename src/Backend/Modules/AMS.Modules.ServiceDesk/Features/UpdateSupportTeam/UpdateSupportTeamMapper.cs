namespace AMS.Modules.ServiceDesk.Features.UpdateSupportTeam;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateSupportTeamMapper
{
    public static UpdateSupportTeamCommand ToCommand(UpdateSupportTeamRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateSupportTeamCommand(
            id,
            request.TeamName.Trim(),
            request.RegionId,
            string.IsNullOrWhiteSpace(request.MailboxAddress) ? null : request.MailboxAddress.Trim(),
            request.IsDefaultTeam ?? false,
            request.IsActive);
    }
}
