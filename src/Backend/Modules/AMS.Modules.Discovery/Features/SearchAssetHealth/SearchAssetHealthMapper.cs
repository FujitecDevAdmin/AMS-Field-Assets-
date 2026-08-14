namespace AMS.Modules.Discovery.Features.SearchAssetHealth;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchAssetHealthMapper
{
    public static SearchAssetHealthQuery ToQuery(SearchAssetHealthRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchAssetHealthQuery(
            request.AssetId,
            request.MinDrivePercent,
            request.NotSeenForHours,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
