namespace AMS.Modules.ServiceDesk.Features.DecideApproval;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record DecideApprovalRequest(
    Guid? ClientDecisionId,
    bool Approved,
    string? Remarks,
    string? Source);
