namespace AMS.Modules.Allocations.Features.MapAssetToSite;

/// <summary>
/// The mapping.
/// </summary>
/// <param name="Id">The mapping.</param>
/// <param name="AssetId">The asset now at the site.</param>
/// <param name="CustomerSiteId">Where it is.</param>
public sealed record MapAssetToSiteResponse(
    int Id,
    int AssetId,
    int CustomerSiteId);
