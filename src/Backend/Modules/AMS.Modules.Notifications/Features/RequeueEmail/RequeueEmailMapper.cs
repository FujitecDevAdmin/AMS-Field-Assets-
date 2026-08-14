namespace AMS.Modules.Notifications.Features.RequeueEmail;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class RequeueEmailMapper
{
    public static RequeueEmailCommand ToCommand(RequeueEmailRequest request, long id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RequeueEmailCommand(
            id);
    }
}
