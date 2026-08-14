namespace AMS.Modules.Allocations.Features.RemoveAssetFromSite;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class RemoveAssetFromSiteMapper
{
    public static RemoveAssetFromSiteCommand ToCommand(RemoveAssetFromSiteRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RemoveAssetFromSiteCommand(
            id);
    }
}
