namespace AMS.Modules.Organization.Features.UpdateRegion;

/// <summary>
/// The updated region.
/// </summary>
/// <param name="Id">The region edited.</param>
/// <param name="RegionName">As stored, trimmed.</param>
/// <param name="IsActive">Retiring is deactivation, never deletion: rows elsewhere still point at this one.</param>
public sealed record UpdateRegionResponse(
    int Id,
    string RegionName,
    bool IsActive);
