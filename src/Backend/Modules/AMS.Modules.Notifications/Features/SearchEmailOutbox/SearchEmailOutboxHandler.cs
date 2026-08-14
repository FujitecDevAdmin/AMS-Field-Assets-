using AMS.Modules.Notifications.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Notifications.Features.SearchEmailOutbox;

/// <summary>
/// What is queued, sent and stuck. Catalogue: the outbox queue.
/// </summary>
/// <remarks>
/// The screen that makes the outbox worth having. Queuing instead of sending
/// inline only helps if somebody can see what did not go out, and the failed
/// count is the number that needs a person.
/// </remarks>
public sealed class SearchEmailOutboxHandler(NotificationsDbContext db)
    : IRequestHandler<SearchEmailOutboxQuery, SearchEmailOutboxResponse>
{
    public async Task<Result<SearchEmailOutboxResponse>> HandleAsync(
        SearchEmailOutboxQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var all = db.EmailOutboxes.AsNoTracking();

        // Counted over the WHOLE queue rather than the filter: they are the
        // health of the outbox, and a reader who has filtered to one ticket
        // still wants to know the queue is on fire.
        var pending = await all.CountAsync(m => m.Status == Domain.OutboxStatus.Pending, ct);
        var failed = await all.CountAsync(m => m.Status == Domain.OutboxStatus.Failed, ct);

        var query = all;

        if (request.Status is { } status)
        {
            query = query.Where(m => m.Status == status);
        }

        if (request.SourceType is { } sourceType)
        {
            query = query.Where(m => m.SourceType == sourceType);
        }

        if (request.SourceId is { } sourceId)
        {
            query = query.Where(m => m.SourceId == sourceId);
        }

        if (request.Search is { } search)
        {
            query = query.Where(m =>
                m.ToAddress.Contains(search) || m.Subject.Contains(search));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(m => m.CreatedOnUtc)
            .ThenByDescending(m => m.Id)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(m => new SearchEmailOutboxResponse.Row(
                m.Id, m.ToAddress, m.CcAddress, m.Subject, m.Status, m.AttemptCount,
                m.LastError, m.SourceType, m.SourceId, m.CreatedOnUtc, m.SentOnUtc))
            .ToListAsync(ct);

        return new SearchEmailOutboxResponse(rows, total, pending, failed);
    }
}
