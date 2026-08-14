namespace AMS.Modules.Allocations.Features.ApproveAcknowledgement;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class ApproveAcknowledgementMapper
{
    public static ApproveAcknowledgementCommand ToCommand(ApproveAcknowledgementRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ApproveAcknowledgementCommand(
            id);
    }
}
