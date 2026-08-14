namespace AMS.Modules.Transfers.Features.RaiseTransfer;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class RaiseTransferMapper
{
    public static RaiseTransferCommand ToCommand(RaiseTransferRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RaiseTransferCommand(
            request.AssetId,
            request.TransferType.Trim(),
            request.ToEmployeeId,
            request.ToDepartmentId,
            request.ToLocationId,
            string.IsNullOrWhiteSpace(request.ToCostCenter) ? null : request.ToCostCenter.Trim(),
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim());
    }
}
