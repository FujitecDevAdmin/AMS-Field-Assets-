namespace AMS.Modules.ServiceDesk.Features.SubmitForApproval;

/// <summary>
/// The approval run, waiting on its first level.
/// </summary>
/// <param name="Id">The run.</param>
/// <param name="ServiceRequestId">The request being approved.</param>
/// <param name="WorkflowName">Copied onto the run, so the audit reads without a join.</param>
/// <param name="WorkflowVersion">Which version is judging it. Fixed for the life of the run.</param>
/// <param name="Status">Always Pending. A run that approved itself would not be a run.</param>
/// <param name="CurrentStageNumber">The level now waiting.</param>
/// <param name="ApproverCount">How many people were resolved into the first level.</param>
public sealed record SubmitForApprovalResponse(
    long Id,
    int ServiceRequestId,
    string WorkflowName,
    int WorkflowVersion,
    string Status,
    int? CurrentStageNumber,
    int ApproverCount);
