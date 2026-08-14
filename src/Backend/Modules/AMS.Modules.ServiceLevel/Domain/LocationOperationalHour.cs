namespace AMS.Modules.ServiceLevel.Domain;

/// <summary>
/// Mirrors <c>[ServiceLevel].[LocationOperationalHour]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
/// <remarks>
/// System-versioned. Prior versions live in <c>[ServiceLevel].[LocationOperationalHourHistory]</c>,
/// readable with <c>TemporalAsOf</c>. The concurrency token is
/// <c>ConcurrencyStamp</c>, NOT the period columns (R2-22).
/// </remarks>
public sealed class LocationOperationalHour
{
    public int Id { get; set; }

    public int LocationId { get; set; }

    public bool IsRoundTheClock { get; set; }

    public TimeOnly StandardStartTime { get; set; }

    public TimeOnly StandardEndTime { get; set; }

    public TimeOnly? BreakStartTime { get; set; }

    public TimeOnly? BreakEndTime { get; set; }

    /// <summary>Defaults to <c>30</c>, as <c>DF_LocationOperationalHour_DeferFinalMinutes</c> does.</summary>
    public int DeferFinalMinutes { get; set; } = 30;

    public bool DeferNewTicketsOnFriday { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    public Guid ConcurrencyStamp { get; set; }
}
