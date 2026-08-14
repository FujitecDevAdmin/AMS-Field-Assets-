namespace AMS.Modules.Organization.Features.SearchApplications;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchApplicationsMapper
{
    public static SearchApplicationsQuery ToQuery(SearchApplicationsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchApplicationsQuery(
            request.IsActive,
            request.Search?.Trim());
    }
}
