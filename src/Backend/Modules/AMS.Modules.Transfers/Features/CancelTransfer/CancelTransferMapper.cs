namespace AMS.Modules.Transfers.Features.CancelTransfer;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CancelTransferMapper
{
    public static CancelTransferCommand ToCommand(CancelTransferRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CancelTransferCommand(
            id,
            request.Reason.Trim());
    }
}
