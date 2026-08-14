namespace AMS.Modules.ServiceLevel.Calendar;

/// <summary>
/// The arithmetic this whole module exists for: is this minute operational for
/// this branch, and what does "four working hours from now" actually mean.
/// </summary>
/// <remarks>
/// <para>
/// Pure functions over a <see cref="CalendarSnapshot"/>. Nothing here touches
/// the database or the clock, so every rule below is testable by stating a
/// calendar and asking a question — which matters more here than anywhere else
/// in this codebase, because an SLA report nobody trusts is worse than no SLA
/// report.
/// </para>
/// <para>
/// <b>Local time throughout.</b> The snapshot's windows are wall-clock times at
/// the branch; the instants crossing this class's edge are UTC. Conversion
/// happens once, on the way in and on the way out. A branch opens at 09:00
/// where it stands, and storing that as UTC breaks twice a year in any country
/// with daylight saving and permanently in any second country.
/// </para>
/// </remarks>
public static class OperationalCalendar
{
    /// <summary>
    /// How far ahead the walkers will look before giving up.
    /// </summary>
    /// <remarks>
    /// A branch with every day marked non-working is a configuration mistake,
    /// not a branch that is shut for ever — but the loops cannot tell the
    /// difference, and without a bound they would spin. Two years is far past
    /// any real answer and close enough to notice.
    /// </remarks>
    private const int SearchLimitDays = 730;

    /// <summary>Whether an instant falls inside the branch's working hours.</summary>
    public static bool IsOperational(CalendarSnapshot calendar, DateTime instantUtc)
    {
        ArgumentNullException.ThrowIfNull(calendar);

        var local = ToLocal(calendar, instantUtc);

        return Windows(calendar, DateOnly.FromDateTime(local))
            .Any(w => TimeOnly.FromDateTime(local) >= w.Start
                && TimeOnly.FromDateTime(local) < w.End);
    }

    /// <summary>Whether the branch works at all on a date.</summary>
    public static bool IsWorkingDay(CalendarSnapshot calendar, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(calendar);

        return Windows(calendar, date).Count > 0;
    }

    /// <summary>Whether a date is a holiday for this branch.</summary>
    /// <remarks>
    /// A recurring holiday is matched on month and day, so Republic Day does
    /// not need re-entering every January. A 29 February recurrence is observed
    /// on 28 February in years that have no 29th — the design script states
    /// this as an application rule precisely so nobody hunts for a missing row.
    /// </remarks>
    public static bool IsHoliday(CalendarSnapshot calendar, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(calendar);

        if (calendar.FixedHolidays.Contains(date))
        {
            return true;
        }

        if (calendar.RecurringHolidays.Contains((date.Month, date.Day)))
        {
            return true;
        }

        return date is { Month: 2, Day: 28 }
            && !DateTime.IsLeapYear(date.Year)
            && calendar.RecurringHolidays.Contains((2, 29));
    }

    /// <summary>
    /// The operational minutes between two instants. Never negative.
    /// </summary>
    /// <remarks>
    /// This is what "a ticket held over a weekend consumes nothing" means in
    /// practice, and what the SLA clock in ServiceDesk charges against once a
    /// policy applies.
    /// </remarks>
    public static int OperationalMinutesBetween(
        CalendarSnapshot calendar,
        DateTime fromUtc,
        DateTime toUtc)
    {
        ArgumentNullException.ThrowIfNull(calendar);

        if (toUtc <= fromUtc)
        {
            return 0;
        }

        var from = ToLocal(calendar, fromUtc);
        var to = ToLocal(calendar, toUtc);

        var total = 0;

        for (var date = DateOnly.FromDateTime(from);
             date <= DateOnly.FromDateTime(to);
             date = date.AddDays(1))
        {
            foreach (var window in Windows(calendar, date))
            {
                var start = date.ToDateTime(window.Start);
                var end = EndOf(date, window.End);

                // Clipped to the span being measured at BOTH ends, so a span
                // starting mid-morning does not get credited with the morning.
                var overlapStart = start > from ? start : from;
                var overlapEnd = end < to ? end : to;

                if (overlapEnd > overlapStart)
                {
                    total += (int)(overlapEnd - overlapStart).TotalMinutes;
                }
            }
        }

        return total;
    }

    /// <summary>
    /// The instant that is <paramref name="minutes"/> operational minutes after
    /// <paramref name="fromUtc"/>, or null if the calendar never gets there.
    /// </summary>
    /// <remarks>
    /// This is how a due date is computed. Zero minutes means "the next moment
    /// the branch is open", which is the right answer for a target of nothing
    /// and the reason the loop starts before the check.
    /// </remarks>
    public static DateTime? AddOperationalMinutes(
        CalendarSnapshot calendar,
        DateTime fromUtc,
        int minutes)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        ArgumentOutOfRangeException.ThrowIfNegative(minutes);

        var from = ToLocal(calendar, fromUtc);
        var remaining = minutes;

        for (var offset = 0; offset <= SearchLimitDays; offset++)
        {
            var date = DateOnly.FromDateTime(from).AddDays(offset);

            foreach (var window in Windows(calendar, date))
            {
                var start = date.ToDateTime(window.Start);
                var end = EndOf(date, window.End);

                if (start < from)
                {
                    start = from;
                }

                if (end <= start)
                {
                    continue;
                }

                var available = (int)(end - start).TotalMinutes;

                if (available > remaining)
                {
                    return ToUtc(calendar, start.AddMinutes(remaining));
                }

                remaining -= available;

                if (remaining == 0)
                {
                    // Landing exactly on a closing time means the deadline is
                    // that moment, not the next morning. A target that lands on
                    // the bell has been met.
                    return ToUtc(calendar, end);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The next instant the branch is open, applying the two intake rules.
    /// </summary>
    /// <param name="calendar">The branch's calendar.</param>
    /// <param name="fromUtc">When the ticket arrived.</param>
    /// <param name="applyDeferralRules">
    /// Whether the branch's own deferral rules apply. A Critical policy usually
    /// ignores them — a production outage does not wait for Monday.
    /// </param>
    /// <returns>
    /// <paramref name="fromUtc"/> itself when the branch is already open and no
    /// rule defers it; otherwise the moment the clock will actually start, or
    /// null if the calendar never opens again.
    /// </returns>
    public static DateTime? NextOperationalStart(
        CalendarSnapshot calendar,
        DateTime fromUtc,
        bool applyDeferralRules = true)
    {
        ArgumentNullException.ThrowIfNull(calendar);

        var from = ToLocal(calendar, fromUtc);
        var deferFrom = from;

        if (applyDeferralRules)
        {
            // "Raised on a Friday goes to Monday." Applied before the final
            // window rule, because a Friday afternoon ticket is deferred by
            // this one whatever the closing time says.
            if (calendar.DeferNewTicketsOnFriday && from.DayOfWeek == DayOfWeek.Friday)
            {
                deferFrom = from.Date.AddDays(1);
            }
            else if (calendar.DeferFinalMinutes > 0)
            {
                // "Raised in the last thirty minutes." Measured against the end
                // of the window it falls in; a ticket raised outside every
                // window is not in anybody's final minutes.
                var closing = Windows(calendar, DateOnly.FromDateTime(from))
                    .Where(w => TimeOnly.FromDateTime(from) >= w.Start
                        && TimeOnly.FromDateTime(from) < w.End)
                    .Select(w => EndOf(DateOnly.FromDateTime(from), w.End))
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();

                if (closing != DateTime.MinValue
                    && (closing - from).TotalMinutes <= calendar.DeferFinalMinutes)
                {
                    deferFrom = closing;
                }
            }
        }

        for (var offset = 0; offset <= SearchLimitDays; offset++)
        {
            var date = DateOnly.FromDateTime(deferFrom).AddDays(offset);

            foreach (var window in Windows(calendar, date))
            {
                var start = date.ToDateTime(window.Start);
                var end = EndOf(date, window.End);

                if (deferFrom >= end)
                {
                    continue;
                }

                return ToUtc(calendar, deferFrom > start ? deferFrom : start);
            }
        }

        return null;
    }

    /// <summary>
    /// The working windows on a local date: at most two, because a break splits
    /// one.
    /// </summary>
    /// <remarks>
    /// Empty means the branch is shut that day — a non-working weekday, a
    /// Saturday whose occurrence is not worked, or a holiday.
    /// </remarks>
    public static IReadOnlyList<(TimeOnly Start, TimeOnly End)> Windows(
        CalendarSnapshot calendar,
        DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(calendar);

        if (IsHoliday(calendar, date))
        {
            return [];
        }

        var day = calendar.Days.FirstOrDefault(d => d.DayOfWeek == date.DayOfWeek);

        if (day is null || !day.IsWorkingDay)
        {
            return [];
        }

        // A Saturday has to satisfy BOTH its weekday row and its occurrence
        // row, which is why the Saturday rules cannot collapse into the weekday
        // table: "we work Saturdays" and "we work the first and third" are
        // different statements and a branch makes both.
        if (date.DayOfWeek == DayOfWeek.Saturday
            && calendar.WorkingSaturdays.Count > 0
            && !calendar.WorkingSaturdays.Contains(OccurrenceInMonth(date)))
        {
            return [];
        }

        if (day.DayType == CalendarDayType.TwentyFourHour
            || (calendar.IsRoundTheClock && day.DayType == CalendarDayType.Standard))
        {
            // TimeOnly cannot hold 24:00, so a full day is stated as
            // MinValue to MaxValue and turned into an instant by EndOf below,
            // which reads MaxValue as the following midnight. Taken literally
            // it would be 23:59:59.9999999, and a round-the-clock day would
            // come out as 1,439 minutes rather than 1,440.
            return [(TimeOnly.MinValue, TimeOnly.MaxValue)];
        }

        var (start, end, breakStart, breakEnd) = day.DayType == CalendarDayType.Custom
            ? (day.Start ?? calendar.StandardStart, day.End ?? calendar.StandardEnd,
                day.BreakStart, day.BreakEnd)
            : (calendar.StandardStart, calendar.StandardEnd,
                calendar.BreakStart, calendar.BreakEnd);

        if (end <= start)
        {
            return [];
        }

        if (breakStart is not { } lunchStart || breakEnd is not { } lunchEnd
            || lunchStart <= start || lunchEnd >= end)
        {
            // A break that covers or falls outside the window removes nothing
            // useful; CK_LocationOperationalHour_BreakInside stops the outside
            // case reaching here at all.
            return [(start, end)];
        }

        return [(start, lunchStart), (lunchEnd, end)];
    }

    /// <summary>
    /// A window's closing time as an instant on that date.
    /// </summary>
    /// <remarks>
    /// <see cref="TimeOnly.MaxValue"/> means "the end of the day", which is the
    /// following midnight and not 23:59:59.9999999. Reading it literally costs
    /// a round-the-clock branch one minute a day, which is invisible until an
    /// SLA report is a minute out and nobody can say why.
    /// </remarks>
    private static DateTime EndOf(DateOnly date, TimeOnly end) =>
        end == TimeOnly.MaxValue
            ? date.AddDays(1).ToDateTime(TimeOnly.MinValue)
            : date.ToDateTime(end);

    /// <summary>Which Saturday of the month this is: 1 through 5.</summary>
    public static int OccurrenceInMonth(DateOnly date) => ((date.Day - 1) / 7) + 1;

    private static DateTime ToLocal(CalendarSnapshot calendar, DateTime instantUtc) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(instantUtc, DateTimeKind.Utc), calendar.TimeZone);

    private static DateTime ToUtc(CalendarSnapshot calendar, DateTime local) =>
        // Unspecified rather than Local: the machine's own zone has nothing to
        // do with where the branch is, and ConvertTimeToUtc refuses a DateTime
        // whose Kind disagrees with the zone it is given.
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(local, DateTimeKind.Unspecified), calendar.TimeZone);
}
