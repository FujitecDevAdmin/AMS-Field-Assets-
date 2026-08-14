namespace AMS.Modules.ServiceDesk.Features.UpdateSupportTeam;

/// <summary>
/// The updated team.
/// </summary>
/// <param name="Id">The team.</param>
/// <param name="TeamName">Unique, trimmed.</param>
/// <param name="IsActive">Retiring hides it from assignment; tickets already with it keep it.</param>
public sealed record UpdateSupportTeamResponse(
    int Id,
    string TeamName,
    bool IsActive);
