namespace AMS.Modules.ServiceDesk.Features.CreateSupportTeam;

/// <summary>
/// The new team.
/// </summary>
/// <param name="Id">The team.</param>
/// <param name="TeamName">Unique, trimmed.</param>
/// <param name="IsDefaultTeam">Exactly one team may be the default — UX_SupportTeam_OneDefault.</param>
public sealed record CreateSupportTeamResponse(
    int Id,
    string TeamName,
    bool IsDefaultTeam);
