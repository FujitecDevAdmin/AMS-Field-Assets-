namespace AMS.Modules.Discovery.Features.SearchInstalledSoftware;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchInstalledSoftwareMapper
{
    public static SearchInstalledSoftwareQuery ToQuery(SearchInstalledSoftwareRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchInstalledSoftwareQuery(
            string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            request.AssetId,
            request.BlacklistedOnly ?? false,
            request.OverLicensedOnly ?? false,
            request.IncludeRemoved ?? false);
    }
}
