namespace AMS.Modules.ServiceDesk.Features.PublishApprovalWorkflow;

/// <summary>
/// Where the definition now stands.
/// </summary>
/// <param name="Id">The definition.</param>
/// <param name="WorkflowName">The route.</param>
/// <param name="VersionNumber">Which version this is.</param>
/// <param name="IsPublished">Whether submissions may pick it up.</param>
/// <param name="IsActive">Whether it is in use at all. Retiring is how a route is replaced.</param>
public sealed record PublishApprovalWorkflowResponse(
    int Id,
    string WorkflowName,
    int VersionNumber,
    bool IsPublished,
    bool IsActive);
