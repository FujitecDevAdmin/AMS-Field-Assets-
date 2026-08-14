namespace AMS.Modules.Allocations.Features.MapAssetToSite;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record MapAssetToSiteRequest(
    int AssetId,
    DateOnly? CommissionedDate);
