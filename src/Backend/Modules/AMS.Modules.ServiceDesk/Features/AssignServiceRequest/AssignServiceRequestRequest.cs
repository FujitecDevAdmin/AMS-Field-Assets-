namespace AMS.Modules.ServiceDesk.Features.AssignServiceRequest;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record AssignServiceRequestRequest(
    int? AssignedToUserId,
    int? AssignedTeamId,
    string? Remarks);
