namespace AMS.Modules.Allocations.Features.CreateCustomerSite;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateCustomerSiteRequest(
    string? CustomerName,
    string SiteName,
    string? City,
    string? Address,
    decimal? Latitude,
    decimal? Longitude);
