namespace AMS.Modules.ServiceLevel.Features.SetLocationCalendar;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SetLocationCalendarRequest(
    bool? IsRoundTheClock,
    TimeOnly? StandardStartTime,
    TimeOnly? StandardEndTime,
    TimeOnly? BreakStartTime,
    TimeOnly? BreakEndTime,
    int? DeferFinalMinutes,
    bool? DeferNewTicketsOnFriday,
    IReadOnlyList<SetLocationCalendarRequest.Day> Days,
    IReadOnlyList<int> WorkingSaturdays)
{
    /// <summary>One weekday, as the setup screen sends it.</summary>
    public sealed record Day(
        byte DayOfWeek,
        bool IsWorkingDay,
        string? DayType,
        TimeOnly? StartTime,
        TimeOnly? EndTime,
        TimeOnly? BreakStartTime,
        TimeOnly? BreakEndTime);
}
