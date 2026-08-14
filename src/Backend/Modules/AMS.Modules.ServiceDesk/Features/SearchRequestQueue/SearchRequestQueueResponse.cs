namespace AMS.Modules.ServiceDesk.Features.SearchRequestQueue;

/// <summary>
/// One page of the queue, worst first.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Tickets matching the filter.</param>
/// <param name="OverdueCount">How many of those have blown their SLA. The number the screen puts in red, counted over the whole filter and not just the page.</param>
public sealed record SearchRequestQueueResponse(
    IReadOnlyList<SearchRequestQueueResponse.Row> Rows,
    int TotalCount,
    int OverdueCount)
{
    /// <summary>One ticket in the queue.</summary>
    /// <param name="Id">The ticket.</param>
    /// <param name="RequestNumber">What the requester quotes.</param>
    /// <param name="RequestKind">SupportTicket, AssetIssue or NewService.</param>
    /// <param name="Subject">The one-line summary.</param>
    /// <param name="Priority">Low, Medium, High or Critical.</param>
    /// <param name="RequestStatusId">Where it is.</param>
    /// <param name="StatusName">Resolved once here rather than by the screen.</param>
    /// <param name="IsClosedState">Whether it is finished.</param>
    /// <param name="CategoryName">Classification, for the column that shows it.</param>
    /// <param name="AssignedToUserId">The technician holding it, if any.</param>
    /// <param name="AssignedTeamId">The team it sits with, if any.</param>
    /// <param name="AssignedTeamName">Resolved for display.</param>
    /// <param name="LocationId">The site.</param>
    /// <param name="RequestedByEmployeeId">Who asked.</param>
    /// <param name="ResponseDueOnUtc">When somebody must have replied by.</param>
    /// <param name="ResolutionDueOnUtc">When it must be fixed by. The sort key, after overdue.</param>
    /// <param name="IsSlaOverdue">The first sort key, and the red row.</param>
    /// <param name="IsSlaPaused">Whether the clock is frozen. A paused ticket is not late; it is waiting.</param>
    /// <param name="CreatedOnUtc">When it was raised.</param>
    public sealed record Row(
        int Id,
        string RequestNumber,
        string RequestKind,
        string Subject,
        string Priority,
        int RequestStatusId,
        string StatusName,
        bool IsClosedState,
        string? CategoryName,
        int? AssignedToUserId,
        int? AssignedTeamId,
        string? AssignedTeamName,
        int? LocationId,
        int RequestedByEmployeeId,
        DateTime? ResponseDueOnUtc,
        DateTime? ResolutionDueOnUtc,
        bool IsSlaOverdue,
        bool IsSlaPaused,
        DateTime CreatedOnUtc);
}
