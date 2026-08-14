namespace AMS.Modules.ServiceDesk.Features.SearchApprovalWorkflows;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchApprovalWorkflowsMapper
{
    public static SearchApprovalWorkflowsQuery ToQuery(SearchApprovalWorkflowsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchApprovalWorkflowsQuery(
            string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            request.PublishedOnly ?? false,
            request.ActiveOnly ?? false,
            request.ServiceTemplateId);
    }
}
