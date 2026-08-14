namespace AMS.Modules.ServiceDesk.Features.ChangeRequestStatus;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record ChangeRequestStatusRequest(
    int RequestStatusId,
    string? Resolution,
    string? Remarks);
