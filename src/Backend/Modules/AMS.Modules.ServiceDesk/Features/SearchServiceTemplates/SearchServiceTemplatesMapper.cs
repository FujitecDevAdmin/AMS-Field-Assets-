namespace AMS.Modules.ServiceDesk.Features.SearchServiceTemplates;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchServiceTemplatesMapper
{
    public static SearchServiceTemplatesQuery ToQuery(SearchServiceTemplatesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchServiceTemplatesQuery(
            request.IsActive,
            string.IsNullOrWhiteSpace(request.RequestKind) ? null : request.RequestKind.Trim());
    }
}
