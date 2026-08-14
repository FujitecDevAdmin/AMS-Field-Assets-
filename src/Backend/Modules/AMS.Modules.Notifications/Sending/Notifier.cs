using AMS.Modules.Notifications.Domain;
using AMS.Modules.Notifications.PublicApi.Notifications;
using AMS.Modules.Notifications.Persistence;
using AMS.SharedKernel.Abstractions;

namespace AMS.Modules.Notifications.Sending;

/// <summary>
/// The only way anything in this system tells somebody something.
/// </summary>
/// <remarks>
/// Queuing is a database insert and nothing else — no SMTP, no HTTP, no
/// waiting. It runs inside the caller's transaction (rule 4a), so a command
/// that rolls back takes its messages with it. A ticket that failed to save
/// having already e-mailed the requester about it is the failure mode this
/// shape exists to prevent.
/// </remarks>
public sealed class Notifier(NotificationsDbContext db, IClock clock) : INotifier
{
    public async Task<long> QueueEmailAsync(OutboundEmail message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        var row = new EmailOutbox
        {
            ToAddress = message.ToAddress,
            CcAddress = message.CcAddress,
            Subject = message.Subject,
            Body = message.Body,
            IsHtml = message.IsHtml,
            Status = OutboxStatus.Pending,
            AttemptCount = 0,
            SourceType = message.SourceType,
            SourceId = message.SourceId,
            CreatedOnUtc = clock.UtcNow,
        };

        db.EmailOutboxes.Add(row);

        // Saved here, not left to the caller: the id IS the return value, and a
        // caller that had to save before reading it would be a caller that
        // knows this module uses EF.
        await db.SaveChangesAsync(ct);

        return row.Id;
    }

    public async Task NotifyAsync(int userId, string text, string? deepLink, CancellationToken ct)
    {
        db.Notifications.Add(Line(userId, text, deepLink));

        await db.SaveChangesAsync(ct);
    }

    public async Task NotifyManyAsync(
        IEnumerable<int> userIds,
        string text,
        string? deepLink,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        // Distinct because the same person can be reached twice by one event —
        // a team lead who is also the assigned technician — and two identical
        // lines in a notification list read as two things happening.
        foreach (var userId in userIds.Distinct())
        {
            db.Notifications.Add(Line(userId, text, deepLink));
        }

        await db.SaveChangesAsync(ct);
    }

    private Notification Line(int userId, string text, string? deepLink) => new()
    {
        UserId = userId,
        // The column is 500; a caller with more to say has a deep link for it.
        Text = text.Length <= 500 ? text : string.Concat(text.AsSpan(0, 497), "..."),
        DeepLink = deepLink,
        IsRead = false,
        CreatedOnUtc = clock.UtcNow,
    };
}
