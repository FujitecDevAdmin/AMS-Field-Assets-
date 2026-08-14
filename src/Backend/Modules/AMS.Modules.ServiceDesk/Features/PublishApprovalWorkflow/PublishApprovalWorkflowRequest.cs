namespace AMS.Modules.ServiceDesk.Features.PublishApprovalWorkflow;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record PublishApprovalWorkflowRequest(
    bool? IsPublished,
    bool? IsActive,
    DateTime? EffectiveFromUtc,
    DateTime? EffectiveToUtc);
