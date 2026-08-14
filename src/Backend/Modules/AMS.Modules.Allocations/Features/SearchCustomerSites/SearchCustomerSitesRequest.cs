namespace AMS.Modules.Allocations.Features.SearchCustomerSites;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchCustomerSitesRequest(
    string? Search,
    bool? IsActive);
