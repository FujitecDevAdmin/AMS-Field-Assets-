namespace AMS.Modules.Allocations.Features.DecideAllocationRequest;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record DecideAllocationRequestRequest(
    bool Approved,
    string? DecisionRemarks);
