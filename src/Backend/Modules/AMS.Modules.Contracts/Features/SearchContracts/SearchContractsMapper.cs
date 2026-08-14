namespace AMS.Modules.Contracts.Features.SearchContracts;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchContractsMapper
{
    public static SearchContractsQuery ToQuery(SearchContractsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchContractsQuery(
            string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            string.IsNullOrWhiteSpace(request.ContractType) ? null : request.ContractType.Trim(),
            request.VendorId,
            request.ExpiringWithinDays,
            request.IncludeExpired ?? false,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
