namespace AMS.Modules.Notifications.PublicApi.Notifications;

/// <summary>
/// The one way anything in this system tells somebody something.
/// </summary>
/// <remarks>
/// <para>
/// Every e-mail goes through the outbox — contract reminders, ticket replies
/// and SLA escalations alike. Sending inline from a request thread loses the
/// message when SMTP is down, and nobody finds out. Queuing is a database
/// insert inside the caller's transaction: if the command rolls back, so does
/// the message, which is the behaviour a reader expects and the one an inline
/// send cannot give.
/// </para>
/// <para>
/// Write-only, deliberately. A module may ask for somebody to be told; none of
/// them may read another user's notifications or another module's queue. Those
/// are Notifications' own screens.
/// </para>
/// </remarks>
public interface INotifier
{
    /// <summary>
    /// Puts a message in the outbox and returns its id.
    /// </summary>
    /// <remarks>
    /// The id is worth keeping: <c>ServiceDesk.RequestEmail.EmailOutboxId</c>
    /// and <c>ServiceDesk.ApprovalNotificationLog.EmailOutboxId</c> both exist
    /// so a module can point at the attempt and say what became of it.
    /// </remarks>
    Task<long> QueueEmailAsync(OutboundEmail message, CancellationToken ct);

    /// <summary>Puts a line in one user's in-app notification list.</summary>
    /// <param name="userId">Identity.User, id only.</param>
    /// <param name="text">What it says. Kept short; the deep link carries the detail.</param>
    /// <param name="deepLink">Where clicking it goes.</param>
    /// <param name="ct">Cancellation.</param>
    Task NotifyAsync(int userId, string text, string? deepLink, CancellationToken ct);

    /// <summary>
    /// The same line for several people, in one round trip.
    /// </summary>
    /// <remarks>
    /// A support team of eight told one at a time is eight round trips for one
    /// event, and escalation tells whole teams.
    /// </remarks>
    Task NotifyManyAsync(
        IEnumerable<int> userIds,
        string text,
        string? deepLink,
        CancellationToken ct);
}
