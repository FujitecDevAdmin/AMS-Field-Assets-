namespace AMS.Modules.Organization.Features.CreateRegion;

/// <summary>
/// The new region.
/// </summary>
/// <param name="Id">The new region.</param>
/// <param name="RegionName">As stored, trimmed.</param>
public sealed record CreateRegionResponse(
    int Id,
    string RegionName);
