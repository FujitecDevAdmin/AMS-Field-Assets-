namespace AMS.Modules.Notifications.Features.SearchMyNotifications;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchMyNotificationsRequest(
    bool? UnreadOnly,
    int? Take);
