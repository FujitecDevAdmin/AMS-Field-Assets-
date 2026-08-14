namespace AMS.Modules.Movements.Features.SearchMovements;

/// <summary>
/// One page of shipments, newest first.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Shipments matching the filter.</param>
public sealed record SearchMovementsResponse(
    IReadOnlyList<SearchMovementsResponse.Row> Rows,
    int TotalCount)
{
    /// <summary>One shipment.</summary>
    /// <param name="Id">The shipment.</param>
    /// <param name="AssetId">What is travelling. Id only — Assets is another module.</param>
    /// <param name="MovementBatchId">The consignment it went on, or null for a single despatch.</param>
    /// <param name="MovementType">Transfer or HandoverToHO.</param>
    /// <param name="FromLocationId">Where it left.</param>
    /// <param name="ToLocationId">Where it is going.</param>
    /// <param name="Status">InTransit, Received or Cancelled.</param>
    /// <param name="Quantity">How much moved. Always 1 for a unit asset.</param>
    /// <param name="CourierName">Who is carrying it.</param>
    /// <param name="TrackingNumber">Their reference.</param>
    /// <param name="ChallanNumber">The delivery challan.</param>
    /// <param name="ShippedOnUtc">When it left.</param>
    /// <param name="ReceivedOnUtc">When it arrived. Null while in transit.</param>
    /// <param name="ReceiptRemarks">What the receiving branch recorded.</param>
    public sealed record Row(
        int Id,
        int AssetId,
        int? MovementBatchId,
        string MovementType,
        int FromLocationId,
        int ToLocationId,
        string Status,
        decimal Quantity,
        string? CourierName,
        string? TrackingNumber,
        string? ChallanNumber,
        DateTime ShippedOnUtc,
        DateTime? ReceivedOnUtc,
        string? ReceiptRemarks);
}
