namespace AMS.Modules.Assets.Features.SearchAssetClasses;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchAssetClassesMapper
{
    public static SearchAssetClassesQuery ToQuery(SearchAssetClassesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchAssetClassesQuery(
            request.IsActive);
    }
}
