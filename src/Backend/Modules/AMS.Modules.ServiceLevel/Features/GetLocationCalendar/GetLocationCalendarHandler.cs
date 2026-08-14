using AMS.Modules.ServiceLevel.Calendar;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;

namespace AMS.Modules.ServiceLevel.Features.GetLocationCalendar;

/// <summary>
/// One branch's working week. Catalogue: Operational Hours Setup.
/// </summary>
/// <remarks>
/// Built from the same <see cref="CalendarSnapshot"/> the SLA arithmetic uses,
/// not from the rows directly. The screen and the clock therefore cannot
/// disagree — including about a branch nobody has configured, where both see
/// the Monday-to-Friday default rather than one seeing nothing.
///
/// The times come back RESOLVED: a Standard day reports the standard window as
/// it currently stands. The stored row still holds nulls, so editing the
/// standard window still moves every Standard day with it; this is what the
/// day keeps today, which is what somebody reading the screen is asking.
/// </remarks>
public sealed class GetLocationCalendarHandler(CalendarLoader calendars)
    : IRequestHandler<GetLocationCalendarQuery, GetLocationCalendarResponse>
{
    public async Task<Result<GetLocationCalendarResponse>> HandleAsync(
        GetLocationCalendarQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var calendar = await calendars.LoadAsync(request.LocationId, ct);
        var configured = await calendars.IsConfiguredAsync(request.LocationId, ct);

        var days = calendar.Days
            .OrderBy(d => (int)d.DayOfWeek)
            .Select(d => Describe(calendar, d))
            .ToList();

        var saturdays = Enumerable.Range(1, 5)
            .Select(occurrence => new GetLocationCalendarResponse.Saturday(
                (byte)occurrence,
                // No rules at all means every Saturday follows the weekday row.
                // Reporting all five as working is what the arithmetic does.
                calendar.WorkingSaturdays.Count == 0
                    || calendar.WorkingSaturdays.Contains(occurrence)))
            .ToList();

        return new GetLocationCalendarResponse(
            calendar.LocationId,
            configured,
            calendar.IsRoundTheClock,
            calendar.StandardStart,
            calendar.StandardEnd,
            calendar.BreakStart,
            calendar.BreakEnd,
            calendar.DeferFinalMinutes,
            calendar.DeferNewTicketsOnFriday,
            days,
            saturdays);
    }

    /// <summary>
    /// One weekday with its window resolved: a Standard day reports the
    /// standard hours as they currently stand.
    /// </summary>
    /// <remarks>
    /// The stored row still holds nulls for a Standard day — that is what makes
    /// editing the standard window move every Standard day with it. This is
    /// what the day keeps TODAY, which is the question somebody reading the
    /// screen is asking.
    ///
    /// Resolved here rather than by borrowing OperationalCalendar.Windows,
    /// because that needs a date, and a date drags holidays into an answer
    /// about a weekday.
    /// </remarks>
    private static GetLocationCalendarResponse.Day Describe(
        CalendarSnapshot calendar,
        CalendarDay day)
    {
        var (start, end, breakStart, breakEnd) = day.DayType switch
        {
            CalendarDayType.TwentyFourHour =>
                ((TimeOnly?)TimeOnly.MinValue, (TimeOnly?)TimeOnly.MaxValue, null, null),

            CalendarDayType.Custom =>
                (day.Start, day.End, day.BreakStart, day.BreakEnd),

            _ when calendar.IsRoundTheClock =>
                (TimeOnly.MinValue, TimeOnly.MaxValue, null, null),

            _ => (calendar.StandardStart, calendar.StandardEnd,
                calendar.BreakStart, calendar.BreakEnd),
        };

        return new GetLocationCalendarResponse.Day(
            (byte)day.DayOfWeek,
            day.DayOfWeek.ToString(),
            day.IsWorkingDay,
            day.DayType,
            day.IsWorkingDay ? start : null,
            day.IsWorkingDay ? end : null,
            day.IsWorkingDay ? breakStart : null,
            day.IsWorkingDay ? breakEnd : null);
    }
}
