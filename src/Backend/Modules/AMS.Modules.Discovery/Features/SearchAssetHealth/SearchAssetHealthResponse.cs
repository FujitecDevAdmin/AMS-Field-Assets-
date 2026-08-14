namespace AMS.Modules.Discovery.Features.SearchAssetHealth;

/// <summary>
/// One page, worst first.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Machines matching the filter.</param>
public sealed record SearchAssetHealthResponse(
    IReadOnlyList<SearchAssetHealthResponse.Row> Rows,
    int TotalCount)
{
    /// <summary>One machine's latest reading.</summary>
    /// <param name="AssetId">The asset.</param>
    /// <param name="Hostname">What the machine calls itself.</param>
    /// <param name="CpuPercent">How busy it was.</param>
    /// <param name="MemoryPercent">How full its memory was.</param>
    /// <param name="SystemDrivePercent">How full its system drive was.</param>
    /// <param name="BatteryHealthPercent">For a laptop.</param>
    /// <param name="UptimeHours">How long since it was restarted.</param>
    /// <param name="LoggedInUser">Who was on it.</param>
    /// <param name="LastSeenOnUtc">When it last reported.</param>
    /// <param name="HoursSinceSeen">
    /// How long ago that was. A machine that has gone quiet is either off, lost
    /// or has had its agent removed, and all three are worth knowing.
    /// </param>
    public sealed record Row(
        int AssetId,
        string Hostname,
        decimal CpuPercent,
        decimal MemoryPercent,
        decimal SystemDrivePercent,
        decimal? BatteryHealthPercent,
        int UptimeHours,
        string? LoggedInUser,
        DateTime LastSeenOnUtc,
        int HoursSinceSeen);
}
