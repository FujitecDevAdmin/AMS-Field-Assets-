namespace AMS.Modules.Movements.Features.DespatchAsset;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record DespatchAssetRequest(
    int AssetId,
    string MovementType,
    int FromLocationId,
    int ToLocationId,
    decimal? Quantity,
    int? HandoverId,
    string? CourierName,
    string? TrackingNumber,
    string? ChallanNumber,
    string? InvoiceNumber,
    DateOnly? InvoiceDate,
    string? Remarks);
