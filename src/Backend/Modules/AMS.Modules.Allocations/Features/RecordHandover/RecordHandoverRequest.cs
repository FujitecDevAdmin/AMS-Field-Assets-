namespace AMS.Modules.Allocations.Features.RecordHandover;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record RecordHandoverRequest(
    int BranchLocationId,
    string ReturnCondition,
    string Remarks,
    IReadOnlyList<string>? ImagePaths);
