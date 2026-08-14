namespace AMS.Modules.Allocations.Features.UpdateCustomerSite;

/// <summary>
/// The updated site.
/// </summary>
/// <param name="Id">The site.</param>
/// <param name="SiteName">As stored.</param>
/// <param name="IsActive">Retiring hides it from pickers; assets already mapped keep pointing here.</param>
public sealed record UpdateCustomerSiteResponse(
    int Id,
    string SiteName,
    bool IsActive);
