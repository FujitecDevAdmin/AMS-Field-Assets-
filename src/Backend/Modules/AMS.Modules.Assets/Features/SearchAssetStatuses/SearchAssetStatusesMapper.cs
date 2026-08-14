namespace AMS.Modules.Assets.Features.SearchAssetStatuses;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchAssetStatusesMapper
{
    public static SearchAssetStatusesQuery ToQuery(SearchAssetStatusesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchAssetStatusesQuery(
            request.IsActive);
    }
}
