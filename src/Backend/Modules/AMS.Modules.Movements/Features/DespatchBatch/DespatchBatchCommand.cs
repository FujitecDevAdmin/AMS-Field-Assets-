using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Movements.Features.DespatchBatch;

/// <summary>
/// Send several assets on one consignment. Catalogue: Despatch several assets at once - one invoice and courier, every asset gets its own tracking row.
/// </summary>
public sealed record DespatchBatchCommand(
    string MovementType,
    int FromLocationId,
    int ToLocationId,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    string CourierName,
    string? TrackingNumber,
    string? ChallanNumber,
    string Remarks,
    IReadOnlyList<int> AssetIds) : ICommand<DespatchBatchResponse>;
