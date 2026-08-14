using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.CreateSupportTeam;

/// <summary>
/// Add a support team. Catalogue: teams with members, so work can go to a queue rather than a person.
/// </summary>
public sealed record CreateSupportTeamCommand(
    string TeamName,
    int? RegionId,
    string? MailboxAddress,
    bool IsDefaultTeam) : ICommand<CreateSupportTeamResponse>;
