using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceLevel.Features.SetLocationCalendar;

/// <summary>
/// Set a branch's working week, all of it at once. Catalogue: Operational Hours Setup.
/// </summary>
public sealed record SetLocationCalendarCommand(
    int LocationId,
    bool IsRoundTheClock,
    TimeOnly StandardStartTime,
    TimeOnly StandardEndTime,
    TimeOnly? BreakStartTime,
    TimeOnly? BreakEndTime,
    int DeferFinalMinutes,
    bool DeferNewTicketsOnFriday,
    IReadOnlyList<SetLocationCalendarCommand.Day> Days,
    IReadOnlyList<int> WorkingSaturdays) : ICommand<SetLocationCalendarResponse>
{
    /// <summary>One weekday of the branch's week.</summary>
    /// <param name="DayOfWeek">0 is Sunday, 6 is Saturday, as the column stores it.</param>
    /// <param name="IsWorkingDay">Whether the branch works it at all.</param>
    /// <param name="DayType">Standard, Custom or TwentyFourHour.</param>
    /// <param name="StartTime">Custom only. Local to the branch.</param>
    /// <param name="EndTime">Custom only.</param>
    /// <param name="BreakStartTime">Custom only.</param>
    /// <param name="BreakEndTime">Custom only.</param>
    /// <remarks>
    /// Standard carries no times on purpose. It means "whatever the standard
    /// window is NOW", and copying the times in at save time would leave seven
    /// stale copies the first time somebody edits that window.
    /// </remarks>
    public sealed record Day(
        byte DayOfWeek,
        bool IsWorkingDay,
        string DayType,
        TimeOnly? StartTime,
        TimeOnly? EndTime,
        TimeOnly? BreakStartTime,
        TimeOnly? BreakEndTime);
}
