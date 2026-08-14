namespace AMS.Modules.ServiceDesk.Features.CancelApproval;

/// <summary>
/// The run, stopped.
/// </summary>
/// <param name="Id">The run.</param>
/// <param name="ServiceRequestId">The request it was for.</param>
/// <param name="Status">Always Cancelled.</param>
/// <param name="CancelledOnUtc">When it was called off.</param>
public sealed record CancelApprovalResponse(
    long Id,
    int ServiceRequestId,
    string Status,
    DateTime CancelledOnUtc);
