namespace AMS.Modules.Transfers.Features.CompleteTransfer;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CompleteTransferMapper
{
    public static CompleteTransferCommand ToCommand(CompleteTransferRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CompleteTransferCommand(
            id,
            request.MovementId);
    }
}
