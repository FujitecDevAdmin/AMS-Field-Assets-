namespace AMS.Modules.Assets.Features.GetAssetTimeline;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetAssetTimelineMapper
{
    public static GetAssetTimelineQuery ToQuery(GetAssetTimelineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetAssetTimelineQuery(
            request.AssetId,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
