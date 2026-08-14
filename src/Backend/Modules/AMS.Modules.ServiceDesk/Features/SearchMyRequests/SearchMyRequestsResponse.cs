namespace AMS.Modules.ServiceDesk.Features.SearchMyRequests;

/// <summary>
/// My tickets, newest first.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Tickets matching the filter.</param>
public sealed record SearchMyRequestsResponse(
    IReadOnlyList<SearchMyRequestsResponse.Row> Rows,
    int TotalCount)
{
    /// <summary>One of my tickets.</summary>
    /// <param name="Id">The ticket.</param>
    /// <param name="RequestNumber">What to quote when chasing it.</param>
    /// <param name="RequestKind">SupportTicket, AssetIssue or NewService.</param>
    /// <param name="Subject">The one-line summary.</param>
    /// <param name="Priority">Low, Medium, High or Critical.</param>
    /// <param name="StatusName">Where it is, in words.</param>
    /// <param name="IsClosedState">Whether it is finished.</param>
    /// <param name="ResolutionDueOnUtc">When it should be fixed by, if a policy applies.</param>
    /// <param name="IsSlaOverdue">Whether that has already passed.</param>
    /// <param name="CreatedOnUtc">When I raised it.</param>
    /// <param name="ClosedOnUtc">When it ended.</param>
    public sealed record Row(
        int Id,
        string RequestNumber,
        string RequestKind,
        string Subject,
        string Priority,
        string StatusName,
        bool IsClosedState,
        DateTime? ResolutionDueOnUtc,
        bool IsSlaOverdue,
        DateTime CreatedOnUtc,
        DateTime? ClosedOnUtc);
}
