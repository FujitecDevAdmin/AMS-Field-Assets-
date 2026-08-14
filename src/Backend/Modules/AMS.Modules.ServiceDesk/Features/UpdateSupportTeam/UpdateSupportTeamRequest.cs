namespace AMS.Modules.ServiceDesk.Features.UpdateSupportTeam;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateSupportTeamRequest(
    string TeamName,
    int? RegionId,
    string? MailboxAddress,
    bool? IsDefaultTeam,
    bool IsActive);
