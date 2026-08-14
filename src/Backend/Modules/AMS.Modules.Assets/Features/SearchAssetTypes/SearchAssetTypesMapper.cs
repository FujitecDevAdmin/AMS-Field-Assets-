namespace AMS.Modules.Assets.Features.SearchAssetTypes;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchAssetTypesMapper
{
    public static SearchAssetTypesQuery ToQuery(SearchAssetTypesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchAssetTypesQuery(
            request.IsActive);
    }
}
