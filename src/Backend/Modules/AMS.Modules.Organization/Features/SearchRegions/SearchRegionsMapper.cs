namespace AMS.Modules.Organization.Features.SearchRegions;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchRegionsMapper
{
    public static SearchRegionsQuery ToQuery(SearchRegionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchRegionsQuery(
            request.IsActive,
            request.Search?.Trim());
    }
}
