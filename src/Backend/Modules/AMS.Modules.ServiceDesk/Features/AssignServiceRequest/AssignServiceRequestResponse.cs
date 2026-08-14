namespace AMS.Modules.ServiceDesk.Features.AssignServiceRequest;

/// <summary>
/// Who holds it now.
/// </summary>
/// <param name="Id">The ticket.</param>
/// <param name="AssignedToUserId">The technician, or null when it sits with a team.</param>
/// <param name="AssignedTeamId">The team.</param>
/// <param name="RequestStatusId">Where it is: assigning an Open ticket moves it to Assigned.</param>
/// <param name="StatusName">Resolved for display.</param>
public sealed record AssignServiceRequestResponse(
    int Id,
    int? AssignedToUserId,
    int? AssignedTeamId,
    int RequestStatusId,
    string StatusName);
