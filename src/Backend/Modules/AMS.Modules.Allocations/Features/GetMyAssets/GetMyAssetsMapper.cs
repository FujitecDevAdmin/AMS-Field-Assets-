namespace AMS.Modules.Allocations.Features.GetMyAssets;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetMyAssetsMapper
{
    public static GetMyAssetsQuery ToQuery(GetMyAssetsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetMyAssetsQuery(
            );
    }
}
