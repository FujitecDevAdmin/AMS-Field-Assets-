namespace AMS.Modules.Allocations.Features.ReceiveReturn;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record ReceiveReturnRequest(
    int? AssetStatusId,
    string? Remarks);
