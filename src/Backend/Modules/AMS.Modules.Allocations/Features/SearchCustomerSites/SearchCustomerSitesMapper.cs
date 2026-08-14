namespace AMS.Modules.Allocations.Features.SearchCustomerSites;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchCustomerSitesMapper
{
    public static SearchCustomerSitesQuery ToQuery(SearchCustomerSitesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchCustomerSitesQuery(
            string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            request.IsActive);
    }
}
