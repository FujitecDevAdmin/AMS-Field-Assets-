namespace AMS.Modules.Allocations.Features.SearchAllocations;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchAllocationsRequest(
    int? AssetId,
    int? EmployeeId,
    int? LocationId,
    bool? OpenOnly,
    bool? OverdueOnly,
    int? Skip,
    int? Take);
