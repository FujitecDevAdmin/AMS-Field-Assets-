namespace AMS.Modules.Transfers.Features.SearchTransferRequests;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchTransferRequestsRequest(
    string? Status,
    string? TransferType,
    int? AssetId,
    string? SapSyncStatus,
    int? Skip,
    int? Take);
