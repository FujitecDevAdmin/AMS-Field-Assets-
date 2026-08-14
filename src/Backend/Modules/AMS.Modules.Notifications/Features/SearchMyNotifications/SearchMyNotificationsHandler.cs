using AMS.Modules.Notifications.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Notifications.Features.SearchMyNotifications;

/// <summary>What I have not read. Catalogue: the notification bell.</summary>
/// <remarks>
/// The unread count is over everything, not the page. A bell showing "3" while
/// there are forty is a bell nobody believes twice.
/// </remarks>
public sealed class SearchMyNotificationsHandler(NotificationsDbContext db)
    : IRequestHandler<SearchMyNotificationsQuery, SearchMyNotificationsResponse>
{
    public async Task<Result<SearchMyNotificationsResponse>> HandleAsync(
        SearchMyNotificationsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mine = db.Notifications.AsNoTracking().Where(n => n.UserId == request.UserId);

        var unread = await mine.CountAsync(n => !n.IsRead, ct);

        var query = request.UnreadOnly ? mine.Where(n => !n.IsRead) : mine;

        var rows = await query
            .OrderByDescending(n => n.CreatedOnUtc)
            .ThenByDescending(n => n.Id)
            .Take(request.Take)
            .Select(n => new SearchMyNotificationsResponse.Row(
                n.Id, n.Text, n.DeepLink, n.IsRead, n.CreatedOnUtc, n.ReadOnUtc))
            .ToListAsync(ct);

        return new SearchMyNotificationsResponse(rows, unread);
    }
}
