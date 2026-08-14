using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Transfers.Features.SearchTransferRequests;

/// <summary>
/// The transfer queue and its SAP status. Catalogue screen: Transfer Requests.
/// </summary>
public sealed record SearchTransferRequestsQuery(
    string? Status,
    string? TransferType,
    int? AssetId,
    string? SapSyncStatus,
    int Skip,
    int Take) : IQuery<SearchTransferRequestsResponse>;
