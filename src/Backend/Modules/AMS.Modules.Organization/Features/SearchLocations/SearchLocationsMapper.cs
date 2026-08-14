namespace AMS.Modules.Organization.Features.SearchLocations;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchLocationsMapper
{
    public static SearchLocationsQuery ToQuery(SearchLocationsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchLocationsQuery(
            request.IsActive,
            request.RegionId,
            request.Search?.Trim());
    }
}
