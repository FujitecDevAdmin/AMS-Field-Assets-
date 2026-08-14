namespace AMS.Modules.ServiceDesk.Features.SearchMyApprovals;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchMyApprovalsRequest(
    bool? PendingOnly,
    int? Skip,
    int? Take);
