namespace AMS.Modules.Organization.Features.UpdateRegion;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateRegionRequest(
    string RegionName,
    string? Description,
    bool IsActive);
