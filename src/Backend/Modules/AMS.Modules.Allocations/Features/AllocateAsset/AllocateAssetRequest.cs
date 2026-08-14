namespace AMS.Modules.Allocations.Features.AllocateAsset;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record AllocateAssetRequest(
    int AssetId,
    int EmployeeId,
    int? LocationId,
    DateOnly? ExpectedReturnDate,
    int? ApprovalId,
    string? Remarks);
