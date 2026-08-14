namespace AMS.Modules.ServiceDesk.Features.SearchMyApprovals;

/// <summary>
/// My approvals, most overdue first.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Approvals matching the filter.</param>
/// <param name="OverdueCount">How many are past their due time, over the whole filter.</param>
public sealed record SearchMyApprovalsResponse(
    IReadOnlyList<SearchMyApprovalsResponse.Row> Rows,
    int TotalCount,
    int OverdueCount)
{
    /// <summary>One thing waiting on me.</summary>
    /// <param name="ParticipantId">The line to decide. This is what DecideApproval takes.</param>
    /// <param name="RequestApprovalInstanceId">The run.</param>
    /// <param name="ServiceRequestId">The request.</param>
    /// <param name="RequestNumber">What the requester quotes.</param>
    /// <param name="Subject">What is being asked for.</param>
    /// <param name="Priority">How urgent they said it was.</param>
    /// <param name="StageNumber">Which level I am.</param>
    /// <param name="StageName">What that level is called.</param>
    /// <param name="ApprovalMode">Any or All — whether my decision alone settles it.</param>
    /// <param name="ParticipantStatus">Where I stand: Pending, or already decided.</param>
    /// <param name="ActivatedOnUtc">When it reached me.</param>
    /// <param name="DueOnUtc">When I am late.</param>
    /// <param name="IsOverdue">Whether I already am.</param>
    /// <param name="SubmittedOnUtc">When the run started.</param>
    public sealed record Row(
        long ParticipantId,
        long RequestApprovalInstanceId,
        int ServiceRequestId,
        string RequestNumber,
        string Subject,
        string Priority,
        int StageNumber,
        string StageName,
        string ApprovalMode,
        string ParticipantStatus,
        DateTime? ActivatedOnUtc,
        DateTime? DueOnUtc,
        bool IsOverdue,
        DateTime SubmittedOnUtc);
}
