namespace AMS.Modules.Assets.Features.SaveAssetDetails;

/// <summary>
/// Which detail records were written.
/// </summary>
/// <param name="AssetId">The asset.</param>
/// <param name="Saved">The detail kinds saved, so the screen can confirm what it did.</param>
public sealed record SaveAssetDetailsResponse(
    int AssetId,
    IReadOnlyList<string> Saved);
