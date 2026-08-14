namespace AMS.Modules.ServiceLevel.Features.SearchSlaPolicies;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchSlaPoliciesMapper
{
    public static SearchSlaPoliciesQuery ToQuery(SearchSlaPoliciesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchSlaPoliciesQuery(
            string.IsNullOrWhiteSpace(request.Priority) ? null : request.Priority.Trim(),
            request.ActiveOnly ?? false);
    }
}
