using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Notifications.Features.MarkNotificationsRead;

/// <summary>
/// Clear the bell, or one line of it. Catalogue: the notification bell.
/// </summary>
public sealed record MarkNotificationsReadCommand(
    int UserId,
    IReadOnlyList<long> Ids,
    bool All) : ICommand<MarkNotificationsReadResponse>;
