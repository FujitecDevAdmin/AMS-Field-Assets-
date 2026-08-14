namespace AMS.Modules.ServiceDesk.Features.SearchApprovalWorkflows;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchApprovalWorkflowsRequest(
    string? Name,
    bool? PublishedOnly,
    bool? ActiveOnly,
    int? ServiceTemplateId);
