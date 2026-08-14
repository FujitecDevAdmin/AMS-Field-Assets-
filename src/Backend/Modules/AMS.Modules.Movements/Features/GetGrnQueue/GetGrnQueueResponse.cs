namespace AMS.Modules.Movements.Features.GetGrnQueue;

/// <summary>
/// Everything in transit to this branch, oldest first.
/// </summary>
/// <param name="Rows">The queue.</param>
/// <param name="TotalCount">Shipments still in transit.</param>
public sealed record GetGrnQueueResponse(
    IReadOnlyList<GetGrnQueueResponse.Row> Rows,
    int TotalCount)
{
    /// <summary>One shipment waiting to be received.</summary>
    /// <param name="Id">The shipment.</param>
    /// <param name="AssetId">What is arriving. Id only.</param>
    /// <param name="MovementBatchId">Its consignment, or null.</param>
    /// <param name="BatchNumber">The consignment number, for the paperwork on the box.</param>
    /// <param name="FromLocationId">Who sent it.</param>
    /// <param name="ToLocationId">Where it is going.</param>
    /// <param name="Quantity">How much is arriving.</param>
    /// <param name="CourierName">Who is carrying it.</param>
    /// <param name="TrackingNumber">Their reference.</param>
    /// <param name="ChallanNumber">The delivery challan, which is what arrives with the box.</param>
    /// <param name="ShippedOnUtc">When it left.</param>
    /// <param name="DaysInTransit">
    /// How long it has been travelling. The queue sorts on it: something sent
    /// three weeks ago and never received is the row worth chasing.
    /// </param>
    public sealed record Row(
        int Id,
        int AssetId,
        int? MovementBatchId,
        string? BatchNumber,
        int FromLocationId,
        int ToLocationId,
        decimal Quantity,
        string? CourierName,
        string? TrackingNumber,
        string? ChallanNumber,
        DateTime ShippedOnUtc,
        int DaysInTransit);
}
