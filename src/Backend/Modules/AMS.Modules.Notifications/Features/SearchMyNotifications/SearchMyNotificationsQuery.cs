using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Notifications.Features.SearchMyNotifications;

/// <summary>
/// What I have not read. Catalogue: the notification bell.
/// </summary>
public sealed record SearchMyNotificationsQuery(
    int UserId,
    bool UnreadOnly,
    int Take) : IQuery<SearchMyNotificationsResponse>;
