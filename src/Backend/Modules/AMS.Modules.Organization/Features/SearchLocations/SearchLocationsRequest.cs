namespace AMS.Modules.Organization.Features.SearchLocations;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchLocationsRequest(
    bool? IsActive,
    int? RegionId,
    string? Search);
