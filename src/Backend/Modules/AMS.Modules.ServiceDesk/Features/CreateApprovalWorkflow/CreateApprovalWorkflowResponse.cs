namespace AMS.Modules.ServiceDesk.Features.CreateApprovalWorkflow;

/// <summary>
/// The draft. It approves nothing until it is published.
/// </summary>
/// <param name="Id">The definition.</param>
/// <param name="WorkflowName">The route's name, shared by every version of it.</param>
/// <param name="VersionNumber">One higher than the highest version of that name.</param>
/// <param name="StageCount">How many levels it has.</param>
public sealed record CreateApprovalWorkflowResponse(
    int Id,
    string WorkflowName,
    int VersionNumber,
    int StageCount);
