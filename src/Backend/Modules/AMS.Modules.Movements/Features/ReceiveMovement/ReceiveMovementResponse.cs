namespace AMS.Modules.Movements.Features.ReceiveMovement;

/// <summary>
/// The received shipment.
/// </summary>
/// <param name="Id">The shipment.</param>
/// <param name="AssetId">The asset, now at the receiving branch.</param>
/// <param name="ToLocationId">Where it arrived — and only now where the asset says it is.</param>
/// <param name="BatchComplete">True when this was the last outstanding item on its consignment.</param>
public sealed record ReceiveMovementResponse(
    int Id,
    int AssetId,
    int ToLocationId,
    bool BatchComplete);
