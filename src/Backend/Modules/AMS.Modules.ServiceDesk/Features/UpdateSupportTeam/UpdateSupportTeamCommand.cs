using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.UpdateSupportTeam;

/// <summary>
/// Edit a team or retire it.
/// </summary>
public sealed record UpdateSupportTeamCommand(
    int Id,
    string TeamName,
    int? RegionId,
    string? MailboxAddress,
    bool IsDefaultTeam,
    bool IsActive) : ICommand<UpdateSupportTeamResponse>;
