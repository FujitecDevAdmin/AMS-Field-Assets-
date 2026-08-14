namespace AMS.Modules.Discovery.Features.SearchDiscoveredDevices;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchDiscoveredDevicesMapper
{
    public static SearchDiscoveredDevicesQuery ToQuery(SearchDiscoveredDevicesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchDiscoveredDevicesQuery(
            string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
            string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            request.UnresolvedOnly ?? false,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
