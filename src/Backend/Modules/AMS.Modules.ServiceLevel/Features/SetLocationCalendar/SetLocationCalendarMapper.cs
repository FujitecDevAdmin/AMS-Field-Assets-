using AMS.Modules.ServiceLevel.Calendar;

namespace AMS.Modules.ServiceLevel.Features.SetLocationCalendar;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SetLocationCalendarMapper
{
    public static SetLocationCalendarCommand ToCommand(SetLocationCalendarRequest request, int locationId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SetLocationCalendarCommand(
            locationId,
            request.IsRoundTheClock ?? false,
            request.StandardStartTime ?? new TimeOnly(9, 0),
            request.StandardEndTime ?? new TimeOnly(18, 0),
            request.BreakStartTime,
            request.BreakEndTime,
            request.DeferFinalMinutes ?? 30,
            request.DeferNewTicketsOnFriday ?? false,
            [.. request.Days.Select(d => new SetLocationCalendarCommand.Day(
                d.DayOfWeek,
                d.IsWorkingDay,
                string.IsNullOrWhiteSpace(d.DayType) ? CalendarDayType.Standard : d.DayType.Trim(),
                d.StartTime,
                d.EndTime,
                d.BreakStartTime,
                d.BreakEndTime))],
            request.WorkingSaturdays);
    }
}
