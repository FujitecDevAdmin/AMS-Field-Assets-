using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Movements.Features.DespatchAsset;

/// <summary>
/// Send one asset to another branch or to head office. Catalogue: Despatch an asset, with courier, tracking and challan.
/// </summary>
public sealed record DespatchAssetCommand(
    int AssetId,
    string MovementType,
    int FromLocationId,
    int ToLocationId,
    decimal Quantity,
    int? HandoverId,
    string? CourierName,
    string? TrackingNumber,
    string? ChallanNumber,
    string? InvoiceNumber,
    DateOnly? InvoiceDate,
    string? Remarks) : ICommand<DespatchAssetResponse>;
