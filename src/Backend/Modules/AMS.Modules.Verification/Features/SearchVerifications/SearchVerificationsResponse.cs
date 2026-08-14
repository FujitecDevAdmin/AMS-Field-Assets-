namespace AMS.Modules.Verification.Features.SearchVerifications;

/// <summary>
/// One page of results, worst first.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Rows matching the filter.</param>
/// <param name="ExceptionCount">How many of those were not Good.</param>
public sealed record SearchVerificationsResponse(
    IReadOnlyList<SearchVerificationsResponse.Row> Rows,
    int TotalCount,
    int ExceptionCount)
{
    /// <summary>One thing that was looked at.</summary>
    /// <param name="Id">The row.</param>
    /// <param name="PhysicalVerificationCycleId">Which round.</param>
    /// <param name="AssetId">What was verified.</param>
    /// <param name="IsBulkCount">
    /// Whether this was a count rather than a sighting. A unit asset is sighted
    /// once per cycle; a bulk line is counted wherever it is held.
    /// </param>
    /// <param name="CountedQuantity">What was on the floor.</param>
    /// <param name="ExpectedQuantitySnapshot">What the sheet said when it was issued.</param>
    /// <param name="Variance">Counted minus expected. Null on a sighting.</param>
    /// <param name="WorkingCondition">Good, MinorDamage, Damaged, NotWorking or Missing.</param>
    /// <param name="HasQrMismatch">Whether the scanned tag belonged to a different asset.</param>
    /// <param name="SerialVerified">Whether the serial was checked as well as the tag.</param>
    /// <param name="LocationId">Where it was found.</param>
    /// <param name="HolderEmployeeId">Who had it.</param>
    /// <param name="GpsLatitude">Where the phone was.</param>
    /// <param name="GpsLongitude">Likewise.</param>
    /// <param name="PhotoPath">The photograph, if one was taken.</param>
    /// <param name="VerifiedByUserId">Who looked.</param>
    /// <param name="VerifiedOnUtc">When. The phone's time, not the server's.</param>
    /// <param name="Remarks">What they said about it.</param>
    public sealed record Row(
        int Id,
        int PhysicalVerificationCycleId,
        int AssetId,
        bool IsBulkCount,
        decimal? CountedQuantity,
        decimal? ExpectedQuantitySnapshot,
        decimal? Variance,
        string WorkingCondition,
        bool HasQrMismatch,
        bool SerialVerified,
        int? LocationId,
        int? HolderEmployeeId,
        decimal? GpsLatitude,
        decimal? GpsLongitude,
        string? PhotoPath,
        int VerifiedByUserId,
        DateTime VerifiedOnUtc,
        string? Remarks);
}
