namespace AMS.Modules.ServiceDesk.Features.SearchSupportTeams;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchSupportTeamsMapper
{
    public static SearchSupportTeamsQuery ToQuery(SearchSupportTeamsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchSupportTeamsQuery(
            request.IsActive,
            request.RegionId);
    }
}
