using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.AssignServiceRequest;

/// <summary>
/// Hand a ticket to a technician, a team, or both. Catalogue: Assign.
/// </summary>
public sealed record AssignServiceRequestCommand(
    int Id,
    int? AssignedToUserId,
    int? AssignedTeamId,
    string? Remarks) : ICommand<AssignServiceRequestResponse>;
