namespace AMS.Modules.Allocations.Features.RequestReturn;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class RequestReturnMapper
{
    public static RequestReturnCommand ToCommand(RequestReturnRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RequestReturnCommand(
            id);
    }
}
