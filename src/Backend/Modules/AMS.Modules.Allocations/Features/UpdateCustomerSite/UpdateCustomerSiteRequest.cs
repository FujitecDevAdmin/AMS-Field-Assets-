namespace AMS.Modules.Allocations.Features.UpdateCustomerSite;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateCustomerSiteRequest(
    string? CustomerName,
    string SiteName,
    string? City,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    bool IsActive);
