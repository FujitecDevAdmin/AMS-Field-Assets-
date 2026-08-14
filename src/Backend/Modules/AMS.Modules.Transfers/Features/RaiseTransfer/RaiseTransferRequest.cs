namespace AMS.Modules.Transfers.Features.RaiseTransfer;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record RaiseTransferRequest(
    int AssetId,
    string TransferType,
    int? ToEmployeeId,
    int? ToDepartmentId,
    int? ToLocationId,
    string? ToCostCenter,
    string? Remarks);
