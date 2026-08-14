namespace AMS.Modules.Notifications.Features.SearchMyNotifications;

/// <summary>
/// My notifications, newest first.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="UnreadCount">The number on the bell. Counted over everything, not the page.</param>
public sealed record SearchMyNotificationsResponse(
    IReadOnlyList<SearchMyNotificationsResponse.Row> Rows,
    int UnreadCount)
{
    /// <summary>One line of the list.</summary>
    /// <param name="Id">The notification.</param>
    /// <param name="Text">What it says.</param>
    /// <param name="DeepLink">Where clicking it goes.</param>
    /// <param name="IsRead">Whether it has been seen.</param>
    /// <param name="CreatedOnUtc">When it arrived.</param>
    /// <param name="ReadOnUtc">When it was cleared.</param>
    public sealed record Row(
        long Id,
        string Text,
        string? DeepLink,
        bool IsRead,
        DateTime CreatedOnUtc,
        DateTime? ReadOnUtc);
}
