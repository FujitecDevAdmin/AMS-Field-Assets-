namespace AMS.Modules.Assets.Features.UpdateAsset;

/// <summary>
/// The updated asset.
/// </summary>
/// <param name="Id">The asset edited.</param>
/// <param name="AssetNumber">Unique, enforced by UX_Asset_Number.</param>
/// <param name="AssetName">As stored.</param>
public sealed record UpdateAssetResponse(
    int Id,
    string AssetNumber,
    string AssetName);
