namespace AMS.Modules.Movements.Features.DespatchBatch;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record DespatchBatchRequest(
    string MovementType,
    int FromLocationId,
    int ToLocationId,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    string CourierName,
    string? TrackingNumber,
    string? ChallanNumber,
    string Remarks,
    IReadOnlyList<int>? AssetIds);
