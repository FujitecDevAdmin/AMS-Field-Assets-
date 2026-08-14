namespace AMS.Modules.ServiceDesk.Features.GetRequestApproval;

/// <summary>
/// The run as the panel draws it.
/// </summary>
/// <param name="Id">The run.</param>
/// <param name="ServiceRequestId">The request.</param>
/// <param name="WorkflowName">Which route, as it was named when this run started.</param>
/// <param name="WorkflowVersion">Which version.</param>
/// <param name="Status">Pending, Approved, Rejected or Cancelled.</param>
/// <param name="CurrentStageNumber">The level now waiting, if the run is still going.</param>
/// <param name="SubmittedByUserId">Who sent it.</param>
/// <param name="SubmittedOnUtc">When.</param>
/// <param name="CompletedOnUtc">When it finished, whichever way.</param>
/// <param name="CancelledOnUtc">When it was called off.</param>
/// <param name="CancellationReason">Why. Required, because a run that stopped for no stated reason is not evidence.</param>
/// <param name="Steps">Every level, in order, each with its approvers and their decisions.</param>
public sealed record GetRequestApprovalResponse(
    long Id,
    int ServiceRequestId,
    string WorkflowName,
    int WorkflowVersion,
    string Status,
    int? CurrentStageNumber,
    int SubmittedByUserId,
    DateTime SubmittedOnUtc,
    DateTime? CompletedOnUtc,
    DateTime? CancelledOnUtc,
    string? CancellationReason,
    IReadOnlyList<GetRequestApprovalResponse.Step> Steps)
{
    /// <summary>One level of the run.</summary>
    /// <param name="Id">The step.</param>
    /// <param name="StageNumber">Its place in the order.</param>
    /// <param name="StageName">As it was named when the run started.</param>
    /// <param name="ApprovalMode">As it was set when the run started.</param>
    /// <param name="Status">Waiting, Pending, Approved, Rejected, Skipped or Cancelled.</param>
    /// <param name="ActivatedOnUtc">When its turn came.</param>
    /// <param name="DueOnUtc">When it falls late.</param>
    /// <param name="CompletedOnUtc">When it finished.</param>
    /// <param name="OutcomeRemarks">What settled it.</param>
    /// <param name="Participants">Who was asked.</param>
    public sealed record Step(
        long Id,
        int StageNumber,
        string StageName,
        string ApprovalMode,
        string Status,
        DateTime? ActivatedOnUtc,
        DateTime? DueOnUtc,
        DateTime? CompletedOnUtc,
        string? OutcomeRemarks,
        IReadOnlyList<Participant> Participants);

    /// <summary>One approver, as they were when the run started.</summary>
    /// <param name="Id">The participant line.</param>
    /// <param name="ApproverUserId">Their account, if they have one.</param>
    /// <param name="ApproverEmployeeId">Their employee record, if known.</param>
    /// <param name="ApproverName">Their name AT SUBMISSION. It does not follow them.</param>
    /// <param name="ApproverEmail">Their address at submission.</param>
    /// <param name="IsRequired">Whether an All level had to wait for them.</param>
    /// <param name="ParticipantStatus">Where they stand.</param>
    /// <param name="Decision">What they decided, if they did.</param>
    public sealed record Participant(
        long Id,
        int? ApproverUserId,
        int? ApproverEmployeeId,
        string ApproverName,
        string ApproverEmail,
        bool IsRequired,
        string ParticipantStatus,
        Decision? Decision);

    /// <summary>What somebody decided, and how we know.</summary>
    /// <param name="Id">The decision row. Append-only; never updated, never deleted.</param>
    /// <param name="Outcome">Approved or Rejected.</param>
    /// <param name="Remarks">What they said.</param>
    /// <param name="ActedByUserId">Who acted, if they had an account.</param>
    /// <param name="ActedByEmail">The address that acted.</param>
    /// <param name="Source">Application, EmailLink or Api — not equally strong evidence.</param>
    /// <param name="DecidedOnUtc">When.</param>
    public sealed record Decision(
        long Id,
        string Outcome,
        string? Remarks,
        int? ActedByUserId,
        string ActedByEmail,
        string Source,
        DateTime DecidedOnUtc);
}
