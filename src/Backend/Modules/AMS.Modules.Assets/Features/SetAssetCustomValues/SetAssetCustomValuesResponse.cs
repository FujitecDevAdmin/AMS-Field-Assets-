namespace AMS.Modules.Assets.Features.SetAssetCustomValues;

/// <summary>
/// How many values were written.
/// </summary>
/// <param name="AssetId">The asset.</param>
/// <param name="SavedCount">Values stored, after blanks were cleared.</param>
public sealed record SetAssetCustomValuesResponse(
    int AssetId,
    int SavedCount);
