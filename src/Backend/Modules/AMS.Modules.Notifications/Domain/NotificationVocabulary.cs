namespace AMS.Modules.Notifications.Domain;

/// <summary>How far a queued message has got.</summary>
/// <remarks>
/// Three states and no more. "Sending" would be a fourth that only a crashed
/// dispatcher ever leaves behind, and a row stuck in it is worse than one that
/// is retried: nothing picks it up and nobody is told.
/// </remarks>
public static class OutboxStatus
{
    /// <summary>Waiting. IX_EmailOutbox_PendingOldest is filtered on this.</summary>
    public const string Pending = "Pending";

    /// <summary>An SMTP server accepted it. Not the same as it reaching an inbox.</summary>
    public const string Sent = "Sent";

    /// <summary>It has been tried enough times. Somebody has to look.</summary>
    public const string Failed = "Failed";

    public static readonly string[] Allowed = [Pending, Sent, Failed];
}
