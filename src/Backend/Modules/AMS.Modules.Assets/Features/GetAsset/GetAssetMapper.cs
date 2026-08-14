namespace AMS.Modules.Assets.Features.GetAsset;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetAssetMapper
{
    public static GetAssetQuery ToQuery(GetAssetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetAssetQuery(
            request.Id);
    }
}
