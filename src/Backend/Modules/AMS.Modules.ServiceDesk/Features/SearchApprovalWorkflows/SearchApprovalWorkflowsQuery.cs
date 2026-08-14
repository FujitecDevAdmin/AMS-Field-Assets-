using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.SearchApprovalWorkflows;

/// <summary>
/// The approval routes and their versions. Catalogue: Approval Workflow Setup.
/// </summary>
public sealed record SearchApprovalWorkflowsQuery(
    string? Name,
    bool PublishedOnly,
    bool ActiveOnly,
    int? ServiceTemplateId) : IQuery<SearchApprovalWorkflowsResponse>;
