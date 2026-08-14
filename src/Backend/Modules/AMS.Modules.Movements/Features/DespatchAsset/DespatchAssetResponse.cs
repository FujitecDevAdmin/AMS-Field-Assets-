namespace AMS.Modules.Movements.Features.DespatchAsset;

/// <summary>
/// The shipment, in transit.
/// </summary>
/// <param name="Id">The shipment.</param>
/// <param name="AssetId">What is travelling.</param>
/// <param name="Status">Always InTransit. The asset's branch does not change until it arrives.</param>
public sealed record DespatchAssetResponse(
    int Id,
    int AssetId,
    string Status);
