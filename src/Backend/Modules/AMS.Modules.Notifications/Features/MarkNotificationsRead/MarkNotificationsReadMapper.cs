namespace AMS.Modules.Notifications.Features.MarkNotificationsRead;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class MarkNotificationsReadMapper
{
    public static MarkNotificationsReadCommand ToCommand(MarkNotificationsReadRequest request, int userId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new MarkNotificationsReadCommand(
            userId,
            request.Ids ?? [],
            request.All ?? false);
    }
}
