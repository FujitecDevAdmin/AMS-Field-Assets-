namespace AMS.Modules.Allocations.Features.ReceiveReturn;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class ReceiveReturnMapper
{
    public static ReceiveReturnCommand ToCommand(ReceiveReturnRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ReceiveReturnCommand(
            id,
            request.AssetStatusId,
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim());
    }
}
