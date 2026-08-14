namespace AMS.Modules.Allocations.Features.SearchCustomerSites;

/// <summary>
/// Every site matching the filter.
/// </summary>
/// <param name="Rows">The sites, by customer then site name.</param>
public sealed record SearchCustomerSitesResponse(
    IReadOnlyList<SearchCustomerSitesResponse.Row> Rows)
{
    /// <summary>One customer site.</summary>
    /// <param name="Id">The site.</param>
    /// <param name="CustomerName">Who the site belongs to. R3 brought this over from FieldAssets.</param>
    /// <param name="SiteName">As stored.</param>
    /// <param name="City">Where it is.</param>
    /// <param name="Address">The full address.</param>
    /// <param name="Latitude">For the map pin, if known.</param>
    /// <param name="Longitude">As above.</param>
    /// <param name="IsActive">Retired sites stay: assets are still mapped to them.</param>
    /// <param name="AssetCount">Assets currently on site.</param>
    public sealed record Row(
        int Id,
        string? CustomerName,
        string SiteName,
        string? City,
        string? Address,
        decimal? Latitude,
        decimal? Longitude,
        bool IsActive,
        int AssetCount);
}
