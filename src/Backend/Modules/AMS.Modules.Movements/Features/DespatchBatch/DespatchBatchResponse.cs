namespace AMS.Modules.Movements.Features.DespatchBatch;

/// <summary>
/// The consignment.
/// </summary>
/// <param name="Id">The batch.</param>
/// <param name="BatchNumber">From MovementBatchNumberSequence. Unique.</param>
/// <param name="ItemCount">How many assets went on it.</param>
public sealed record DespatchBatchResponse(
    int Id,
    string BatchNumber,
    int ItemCount);
