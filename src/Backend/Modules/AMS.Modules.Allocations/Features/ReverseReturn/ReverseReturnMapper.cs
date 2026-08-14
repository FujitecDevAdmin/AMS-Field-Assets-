namespace AMS.Modules.Allocations.Features.ReverseReturn;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class ReverseReturnMapper
{
    public static ReverseReturnCommand ToCommand(ReverseReturnRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ReverseReturnCommand(
            id,
            request.Reason.Trim());
    }
}
