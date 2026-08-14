using AMS.Modules.Notifications.PublicApi.Notifications;

namespace AMS.Modules.ServiceDesk.Tests;

/// <summary>
/// The outbox, as far as ServiceDesk is concerned: a list of what it was asked
/// to send.
/// </summary>
/// <remarks>
/// A stub, because whether a message reaches a mail server is Notifications'
/// question — tested there, against a transport that can be made to fail. What
/// ServiceDesk has to get right is that it asks at all, with the right source
/// and the right recipient, and that it keeps the id it gets back.
/// </remarks>
public sealed class FakeNotifier : INotifier
{
    private long _nextId = 5000;

    /// <summary>Everything queued, in order.</summary>
    public List<OutboundEmail> Queued { get; } = [];

    /// <summary>Every in-app line, as (user, text) pairs.</summary>
    public List<(int UserId, string Text)> Notified { get; } = [];

    public void Reset()
    {
        Queued.Clear();
        Notified.Clear();
    }

    public Task<long> QueueEmailAsync(OutboundEmail message, CancellationToken ct)
    {
        Queued.Add(message);

        return Task.FromResult(_nextId++);
    }

    public Task NotifyAsync(int userId, string text, string? deepLink, CancellationToken ct)
    {
        Notified.Add((userId, text));

        return Task.CompletedTask;
    }

    public Task NotifyManyAsync(
        IEnumerable<int> userIds,
        string text,
        string? deepLink,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        foreach (var userId in userIds.Distinct())
        {
            Notified.Add((userId, text));
        }

        return Task.CompletedTask;
    }
}
