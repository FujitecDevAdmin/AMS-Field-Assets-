namespace AMS.Modules.Allocations.Features.SearchAllocationRequests;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchAllocationRequestsRequest(
    string? Status,
    int? EmployeeId,
    int? Skip,
    int? Take);
