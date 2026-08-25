namespace AMS.Modules.Organization.Features.SearchBranches;

/// <summary>Every branch matching the filter.</summary>
/// <param name="Rows">The branches.</param>
public sealed record SearchBranchesResponse(IReadOnlyList<SearchBranchesResponse.Row> Rows)
{
    /// <summary>One branch.</summary>
    /// <param name="Id">The branch.</param>
    /// <param name="BranchCode">Unique, upper-cased.</param>
    /// <param name="BranchName">As stored.</param>
    /// <param name="RegionId">Null until somebody puts the branch in a region.</param>
    /// <param name="RegionName">Denormalised for the grid; null when RegionId is.</param>
    /// <param name="Latitude">Branch latitude in decimal degrees.</param>
    /// <param name="Longitude">Branch longitude in decimal degrees.</param>
    /// <param name="TimeZoneId">
    /// What 09:00 means at this branch. Every SLA measurement taken against the
    /// branch depends on it.
    /// </param>
    /// <param name="IsHeadOffice">At most one across the system.</param>
    /// <param name="IsActive">Retired branches stay, because assets and employees point at them.</param>
    public sealed record Row(
        int Id,
        string BranchCode,
        string BranchName,
        int? RegionId,
        string? RegionName,
        decimal? Latitude,
        decimal? Longitude,
        string TimeZoneId,
        bool IsHeadOffice,
        bool IsActive);
}
