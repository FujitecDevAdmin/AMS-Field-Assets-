namespace AMS.Modules.Assets.Features.RegisterAsset;

/// <summary>
/// The new asset.
/// </summary>
/// <param name="Id">The new asset.</param>
/// <param name="AssetNumber">Unique, enforced by UX_Asset_Number.</param>
/// <param name="AssetName">As stored.</param>
public sealed record RegisterAssetResponse(
    int Id,
    string AssetNumber,
    string AssetName);
