namespace AMS.Modules.Transfers.Features.SearchTransferRequests;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchTransferRequestsMapper
{
    public static SearchTransferRequestsQuery ToQuery(SearchTransferRequestsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchTransferRequestsQuery(
            string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
            string.IsNullOrWhiteSpace(request.TransferType) ? null : request.TransferType.Trim(),
            request.AssetId,
            string.IsNullOrWhiteSpace(request.SapSyncStatus) ? null : request.SapSyncStatus.Trim(),
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
