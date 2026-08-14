namespace AMS.Modules.Allocations.Features.MapAssetToSite;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class MapAssetToSiteMapper
{
    public static MapAssetToSiteCommand ToCommand(MapAssetToSiteRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new MapAssetToSiteCommand(
            id,
            request.AssetId,
            request.CommissionedDate);
    }
}
