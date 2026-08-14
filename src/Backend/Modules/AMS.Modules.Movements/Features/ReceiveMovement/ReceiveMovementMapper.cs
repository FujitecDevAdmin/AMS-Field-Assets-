namespace AMS.Modules.Movements.Features.ReceiveMovement;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class ReceiveMovementMapper
{
    public static ReceiveMovementCommand ToCommand(ReceiveMovementRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ReceiveMovementCommand(
            id,
            string.IsNullOrWhiteSpace(request.ReceiptRemarks) ? null : request.ReceiptRemarks.Trim());
    }
}
