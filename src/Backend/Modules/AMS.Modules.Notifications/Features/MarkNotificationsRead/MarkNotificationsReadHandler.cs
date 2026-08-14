using AMS.Modules.Notifications.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Notifications.Features.MarkNotificationsRead;

/// <summary>Clear the bell. Catalogue: the notification bell.</summary>
/// <remarks>
/// Scoped to the caller's own rows in the WHERE clause, not checked afterwards.
/// An id belonging to somebody else simply matches nothing, which is the right
/// answer and also the one that cannot be turned into a way of finding out
/// whether that id exists.
/// </remarks>
public sealed class MarkNotificationsReadHandler(
    NotificationsDbContext db,
    IClock clock)
    : IRequestHandler<MarkNotificationsReadCommand, MarkNotificationsReadResponse>
{
    public async Task<Result<MarkNotificationsReadResponse>> HandleAsync(
        MarkNotificationsReadCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.All && request.Ids.Count == 0)
        {
            return Error.Validation(
                "Notification.NothingToMark",
                "Name the notifications to clear, or ask for all of them.");
        }

        var target = db.Notifications.Where(n => n.UserId == request.UserId && !n.IsRead);

        if (!request.All)
        {
            var ids = request.Ids.Distinct().ToList();
            target = target.Where(n => ids.Contains(n.Id));
        }

        var rows = await target.ToListAsync(ct);
        var now = clock.UtcNow;

        foreach (var row in rows)
        {
            row.IsRead = true;
            row.ReadOnUtc = now;
        }

        await db.SaveChangesAsync(ct);

        var unread = await db.Notifications
            .CountAsync(n => n.UserId == request.UserId && !n.IsRead, ct);

        return new MarkNotificationsReadResponse(rows.Count, unread);
    }
}
