namespace AMS.Modules.Movements.Features.DespatchAsset;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class DespatchAssetMapper
{
    public static DespatchAssetCommand ToCommand(DespatchAssetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new DespatchAssetCommand(
            request.AssetId,
            request.MovementType.Trim(),
            request.FromLocationId,
            request.ToLocationId,
            request.Quantity ?? 1m,
            request.HandoverId,
            string.IsNullOrWhiteSpace(request.CourierName) ? null : request.CourierName.Trim(),
            string.IsNullOrWhiteSpace(request.TrackingNumber) ? null : request.TrackingNumber.Trim(),
            string.IsNullOrWhiteSpace(request.ChallanNumber) ? null : request.ChallanNumber.Trim(),
            string.IsNullOrWhiteSpace(request.InvoiceNumber) ? null : request.InvoiceNumber.Trim(),
            request.InvoiceDate,
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim());
    }
}
