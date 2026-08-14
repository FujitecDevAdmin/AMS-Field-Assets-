namespace AMS.Modules.ServiceLevel.Domain;

/// <summary>
/// Mirrors <c>[ServiceLevel].[LocationOperationalDay]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class LocationOperationalDay
{
    public int Id { get; set; }

    public int LocationOperationalHourId { get; set; }

    public byte DayOfWeek { get; set; }

    public bool IsWorkingDay { get; set; }

    public required string DayType { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public TimeOnly? BreakStartTime { get; set; }

    public TimeOnly? BreakEndTime { get; set; }
}
