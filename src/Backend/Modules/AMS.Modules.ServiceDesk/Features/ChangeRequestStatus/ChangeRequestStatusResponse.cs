namespace AMS.Modules.ServiceDesk.Features.ChangeRequestStatus;

/// <summary>
/// Where the ticket is now, and what its clock did.
/// </summary>
/// <param name="Id">The ticket.</param>
/// <param name="RequestStatusId">Where it is now.</param>
/// <param name="StatusName">Resolved for display.</param>
/// <param name="IsClosedState">Whether it is finished.</param>
/// <param name="IsSlaPaused">Whether the new status freezes the clock.</param>
/// <param name="ResolutionConsumedMinutes">Minutes charged so far, updated by this move.</param>
public sealed record ChangeRequestStatusResponse(
    int Id,
    int RequestStatusId,
    string StatusName,
    bool IsClosedState,
    bool IsSlaPaused,
    int ResolutionConsumedMinutes);
