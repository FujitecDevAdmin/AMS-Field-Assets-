namespace AMS.Modules.Notifications.Features.SearchMyNotifications;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchMyNotificationsMapper
{
    public static SearchMyNotificationsQuery ToQuery(SearchMyNotificationsRequest request, int userId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchMyNotificationsQuery(
            userId,
            request.UnreadOnly ?? false,
            request.Take ?? 50);
    }
}
