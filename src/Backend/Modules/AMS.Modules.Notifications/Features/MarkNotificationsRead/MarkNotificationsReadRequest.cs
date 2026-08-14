namespace AMS.Modules.Notifications.Features.MarkNotificationsRead;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record MarkNotificationsReadRequest(
    IReadOnlyList<long>? Ids,
    bool? All);
