namespace AMS.Modules.Transfers.Features.DecideTransfer;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class DecideTransferMapper
{
    public static DecideTransferCommand ToCommand(DecideTransferRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new DecideTransferCommand(
            id,
            request.Approved,
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim());
    }
}
