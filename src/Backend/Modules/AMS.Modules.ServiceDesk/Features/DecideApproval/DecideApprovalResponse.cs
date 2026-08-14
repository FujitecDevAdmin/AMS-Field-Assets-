namespace AMS.Modules.ServiceDesk.Features.DecideApproval;

/// <summary>
/// What the decision did to the level and to the run.
/// </summary>
/// <param name="ParticipantId">The approver line that was decided.</param>
/// <param name="Decision">Approved or Rejected.</param>
/// <param name="StepStatus">Where the level stands now.</param>
/// <param name="InstanceStatus">Where the whole run stands now.</param>
/// <param name="CurrentStageNumber">The next level, if the run moved on.</param>
/// <param name="WasAlreadyDecided">True when this call replayed a decision already recorded under the same ClientDecisionId. The answer is the same one, not a second decision.</param>
public sealed record DecideApprovalResponse(
    long ParticipantId,
    string Decision,
    string StepStatus,
    string InstanceStatus,
    int? CurrentStageNumber,
    bool WasAlreadyDecided);
