namespace AMS.Modules.Allocations.Features.CreateCustomerSite;

/// <summary>
/// The new site.
/// </summary>
/// <param name="Id">The site.</param>
/// <param name="SiteName">As stored.</param>
public sealed record CreateCustomerSiteResponse(
    int Id,
    string SiteName);
