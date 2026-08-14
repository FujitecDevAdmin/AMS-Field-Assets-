namespace AMS.Modules.ServiceDesk.Features.SearchRequestQueue;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchRequestQueueRequest(
    string? RequestKind,
    int? RequestStatusId,
    string? Priority,
    int? AssignedToUserId,
    int? AssignedTeamId,
    int? LocationId,
    bool? Unassigned,
    bool? OverdueOnly,
    bool? OpenOnly,
    string? Search,
    int? Skip,
    int? Take);
