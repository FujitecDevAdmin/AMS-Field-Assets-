namespace AMS.Modules.ServiceLevel.Features.GetLocationCalendar;

/// <summary>
/// The branch's week, configured or defaulted.
/// </summary>
/// <param name="LocationId">The branch.</param>
/// <param name="IsConfigured">False when nobody has set this branch up and the screen is showing the Monday-to-Friday default.</param>
/// <param name="IsRoundTheClock">A 24-hour branch. The windows below do not apply.</param>
/// <param name="StandardStartTime">When it opens, local to the branch.</param>
/// <param name="StandardEndTime">When it closes.</param>
/// <param name="BreakStartTime">Lunch, if it takes one.</param>
/// <param name="BreakEndTime">The end of lunch.</param>
/// <param name="DeferFinalMinutes">A ticket raised this close to closing starts its clock tomorrow.</param>
/// <param name="DeferNewTicketsOnFriday">A ticket raised on a Friday starts its clock on Monday.</param>
/// <param name="Days">Seven rows, Sunday first.</param>
/// <param name="Saturdays">Which Saturdays of the month are worked.</param>
public sealed record GetLocationCalendarResponse(
    int LocationId,
    bool IsConfigured,
    bool IsRoundTheClock,
    TimeOnly StandardStartTime,
    TimeOnly StandardEndTime,
    TimeOnly? BreakStartTime,
    TimeOnly? BreakEndTime,
    int DeferFinalMinutes,
    bool DeferNewTicketsOnFriday,
    IReadOnlyList<GetLocationCalendarResponse.Day> Days,
    IReadOnlyList<GetLocationCalendarResponse.Saturday> Saturdays)
{
    /// <summary>One weekday.</summary>
    /// <param name="DayOfWeek">0 is Sunday.</param>
    /// <param name="DayName">Spelled out, so the screen need not.</param>
    /// <param name="IsWorkingDay">Whether it is worked.</param>
    /// <param name="DayType">Standard, Custom or TwentyFourHour.</param>
    /// <param name="StartTime">The hours this day actually keeps, standard window resolved.</param>
    /// <param name="EndTime">Likewise.</param>
    /// <param name="BreakStartTime">Lunch, resolved the same way.</param>
    /// <param name="BreakEndTime">The end of lunch.</param>
    public sealed record Day(
        byte DayOfWeek,
        string DayName,
        bool IsWorkingDay,
        string DayType,
        TimeOnly? StartTime,
        TimeOnly? EndTime,
        TimeOnly? BreakStartTime,
        TimeOnly? BreakEndTime);

    /// <summary>One Saturday of the month.</summary>
    /// <param name="Occurrence">1 through 5.</param>
    /// <param name="IsWorking">Whether that Saturday is worked.</param>
    public sealed record Saturday(byte Occurrence, bool IsWorking);
}
