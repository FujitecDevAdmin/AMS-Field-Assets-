namespace AMS.Modules.Notifications.Features.MarkNotificationsRead;

/// <summary>
/// How many were cleared, and what is left.
/// </summary>
/// <param name="MarkedCount">How many changed. Already-read lines are not counted twice.</param>
/// <param name="UnreadCount">The number the bell should now show.</param>
public sealed record MarkNotificationsReadResponse(
    int MarkedCount,
    int UnreadCount);
