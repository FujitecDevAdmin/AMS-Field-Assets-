namespace AMS.Modules.Organization.Features.SearchLocations;

/// <summary>Every branch matching the filter.</summary>
/// <param name="Rows">The branches.</param>
public sealed record SearchLocationsResponse(IReadOnlyList<SearchLocationsResponse.Row> Rows)
{
    /// <summary>One branch.</summary>
    /// <param name="Id">The branch.</param>
    /// <param name="LocationCode">Unique, upper-cased.</param>
    /// <param name="LocationName">As stored.</param>
    /// <param name="RegionId">Null until somebody puts the branch in a region.</param>
    /// <param name="RegionName">Denormalised for the grid; null when RegionId is.</param>
    /// <param name="TimeZoneId">
    /// What 09:00 means at this branch. Every SLA measurement taken against the
    /// branch depends on it.
    /// </param>
    /// <param name="IsHeadOffice">At most one across the system.</param>
    /// <param name="IsActive">Retired branches stay, because assets and employees point at them.</param>
    public sealed record Row(
        int Id,
        string LocationCode,
        string LocationName,
        int? RegionId,
        string? RegionName,
        string TimeZoneId,
        bool IsHeadOffice,
        bool IsActive);
}
