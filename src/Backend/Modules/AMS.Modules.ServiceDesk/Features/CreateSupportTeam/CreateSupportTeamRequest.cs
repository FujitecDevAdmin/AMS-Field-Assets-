namespace AMS.Modules.ServiceDesk.Features.CreateSupportTeam;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateSupportTeamRequest(
    string TeamName,
    int? RegionId,
    string? MailboxAddress,
    bool? IsDefaultTeam);
