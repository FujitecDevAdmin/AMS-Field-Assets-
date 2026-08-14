namespace AMS.Modules.Allocations.Features.RequestAllocation;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record RequestAllocationRequest(
    int AssetId,
    int EmployeeId,
    int? LocationId,
    string? Remarks);
