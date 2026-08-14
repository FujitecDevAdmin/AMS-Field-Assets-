namespace AMS.Modules.Assets.Features.DeleteAsset;

/// <summary>
/// The asset, now marked deleted.
/// </summary>
/// <param name="Id">The asset removed.</param>
/// <param name="IsDeleted">Always true. The row and its timeline stay.</param>
public sealed record DeleteAssetResponse(
    int Id,
    bool IsDeleted);
