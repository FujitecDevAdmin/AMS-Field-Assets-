using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.PublishApprovalWorkflow;

/// <summary>
/// Publish a draft route, or retire a published one. Catalogue: Approval Workflow Setup.
/// </summary>
public sealed record PublishApprovalWorkflowCommand(
    int Id,
    bool IsPublished,
    bool IsActive,
    DateTime? EffectiveFromUtc,
    DateTime? EffectiveToUtc) : ICommand<PublishApprovalWorkflowResponse>;
