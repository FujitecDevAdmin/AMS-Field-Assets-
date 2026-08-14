namespace AMS.Modules.Organization.Features.SearchVendors;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchVendorsMapper
{
    public static SearchVendorsQuery ToQuery(SearchVendorsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchVendorsQuery(
            request.IsActive,
            request.Search?.Trim());
    }
}
