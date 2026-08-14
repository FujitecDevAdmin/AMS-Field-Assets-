namespace AMS.Modules.Notifications.Features.SearchEmailOutbox;

/// <summary>
/// The queue, newest first.
/// </summary>
/// <param name="Rows">The page. Bodies are not included; the list is a list.</param>
/// <param name="TotalCount">Messages matching the filter.</param>
/// <param name="PendingCount">How many are still waiting, over the whole queue.</param>
/// <param name="FailedCount">How many have been given up on. The number that needs somebody.</param>
public sealed record SearchEmailOutboxResponse(
    IReadOnlyList<SearchEmailOutboxResponse.Row> Rows,
    int TotalCount,
    int PendingCount,
    int FailedCount)
{
    /// <summary>One queued message.</summary>
    /// <param name="Id">The message.</param>
    /// <param name="ToAddress">Where it goes.</param>
    /// <param name="CcAddress">Who else sees it.</param>
    /// <param name="Subject">The subject line.</param>
    /// <param name="Status">Pending, Sent or Failed.</param>
    /// <param name="AttemptCount">How many times it has been tried.</param>
    /// <param name="LastError">Why the last attempt failed.</param>
    /// <param name="SourceType">What asked for it: ServiceRequest, Contract, SlaEscalation, Approval.</param>
    /// <param name="SourceId">The id of that thing, so a bounced message leads back to it.</param>
    /// <param name="CreatedOnUtc">When it was queued.</param>
    /// <param name="SentOnUtc">When an SMTP server accepted it.</param>
    /// <remarks>
    /// No body. The list is a list, and a queue screen that carried every
    /// message body would move megabytes to draw fifty rows.
    /// </remarks>
    public sealed record Row(
        long Id,
        string ToAddress,
        string? CcAddress,
        string Subject,
        string Status,
        int AttemptCount,
        string? LastError,
        string? SourceType,
        long? SourceId,
        DateTime CreatedOnUtc,
        DateTime? SentOnUtc);
}
