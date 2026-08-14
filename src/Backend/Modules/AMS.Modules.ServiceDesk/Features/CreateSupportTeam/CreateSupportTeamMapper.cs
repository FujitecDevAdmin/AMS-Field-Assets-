namespace AMS.Modules.ServiceDesk.Features.CreateSupportTeam;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateSupportTeamMapper
{
    public static CreateSupportTeamCommand ToCommand(CreateSupportTeamRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateSupportTeamCommand(
            request.TeamName.Trim(),
            request.RegionId,
            string.IsNullOrWhiteSpace(request.MailboxAddress) ? null : request.MailboxAddress.Trim(),
            request.IsDefaultTeam ?? false);
    }
}
