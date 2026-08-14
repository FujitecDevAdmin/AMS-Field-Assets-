namespace AMS.Modules.Movements.Features.DespatchBatch;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class DespatchBatchMapper
{
    public static DespatchBatchCommand ToCommand(DespatchBatchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new DespatchBatchCommand(
            request.MovementType.Trim(),
            request.FromLocationId,
            request.ToLocationId,
            request.InvoiceNumber.Trim(),
            request.InvoiceDate,
            request.CourierName.Trim(),
            string.IsNullOrWhiteSpace(request.TrackingNumber) ? null : request.TrackingNumber.Trim(),
            string.IsNullOrWhiteSpace(request.ChallanNumber) ? null : request.ChallanNumber.Trim(),
            request.Remarks.Trim(),
            request.AssetIds ?? []);
    }
}
