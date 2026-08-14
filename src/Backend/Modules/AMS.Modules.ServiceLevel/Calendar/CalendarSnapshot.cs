namespace AMS.Modules.ServiceLevel.Calendar;

/// <summary>One branch's calendar, flattened into what the arithmetic needs.</summary>
/// <param name="LocationId">The branch.</param>
/// <param name="TimeZone">
/// Where it stands. Everything in this record is LOCAL to it; the only instants
/// in this module are the ones crossing its edge.
/// </param>
/// <param name="IsRoundTheClock">A 24-hour branch. The windows below are ignored.</param>
/// <param name="StandardStart">When it opens, on a Standard day.</param>
/// <param name="StandardEnd">When it closes.</param>
/// <param name="BreakStart">Lunch, if it takes one.</param>
/// <param name="BreakEnd">The end of lunch.</param>
/// <param name="DeferFinalMinutes">
/// A ticket raised inside this many minutes of closing does not start its clock
/// today. The handbook's rule, and configuration rather than code: a branch
/// manager has to be able to turn it off when it stops matching how the branch
/// works.
/// </param>
/// <param name="DeferNewTicketsOnFriday">The other one: raised on a Friday, starts Monday.</param>
/// <param name="Days">Seven rows, indexed by <see cref="DayOfWeek"/>.</param>
/// <param name="WorkingSaturdays">Which Saturdays of the month are worked.</param>
/// <param name="FixedHolidays">Dates that are not worked, this year.</param>
/// <param name="RecurringHolidays">Month and day pairs that are not worked, any year.</param>
public sealed record CalendarSnapshot(
    int LocationId,
    TimeZoneInfo TimeZone,
    bool IsRoundTheClock,
    TimeOnly StandardStart,
    TimeOnly StandardEnd,
    TimeOnly? BreakStart,
    TimeOnly? BreakEnd,
    int DeferFinalMinutes,
    bool DeferNewTicketsOnFriday,
    IReadOnlyList<CalendarDay> Days,
    IReadOnlySet<int> WorkingSaturdays,
    IReadOnlySet<DateOnly> FixedHolidays,
    IReadOnlySet<(int Month, int Day)> RecurringHolidays)
{
    /// <summary>
    /// The calendar a branch with no configuration gets: Monday to Friday,
    /// 09:00 to 18:00.
    /// </summary>
    /// <remarks>
    /// Stated in the design script, and it has to exist: a branch nobody has
    /// configured yet still raises tickets, and a calendar that answered
    /// "never operational" would make every one of them instantly overdue.
    /// </remarks>
    public static CalendarSnapshot Default(int locationId, TimeZoneInfo timeZone) =>
        new(locationId,
            timeZone,
            IsRoundTheClock: false,
            new TimeOnly(9, 0),
            new TimeOnly(18, 0),
            BreakStart: null,
            BreakEnd: null,
            DeferFinalMinutes: 0,
            DeferNewTicketsOnFriday: false,
            [.. Enum.GetValues<DayOfWeek>().Select(day => new CalendarDay(
                day,
                day is not (DayOfWeek.Saturday or DayOfWeek.Sunday),
                CalendarDayType.Standard,
                null, null, null, null))],
            new HashSet<int>(),
            new HashSet<DateOnly>(),
            new HashSet<(int, int)>());
}

/// <summary>One weekday of a branch's week.</summary>
/// <param name="DayOfWeek">Which day.</param>
/// <param name="IsWorkingDay">Whether it is worked at all.</param>
/// <param name="DayType">Standard, Custom or TwentyFourHour.</param>
/// <param name="Start">Custom only.</param>
/// <param name="End">Custom only.</param>
/// <param name="BreakStart">Custom only.</param>
/// <param name="BreakEnd">Custom only.</param>
/// <remarks>
/// <c>Standard</c> means "whatever the standard hours are NOW", which is why
/// the times are null rather than copied. Copying them at save time leaves
/// seven stale copies the first time somebody edits the standard window.
/// </remarks>
public sealed record CalendarDay(
    DayOfWeek DayOfWeek,
    bool IsWorkingDay,
    string DayType,
    TimeOnly? Start,
    TimeOnly? End,
    TimeOnly? BreakStart,
    TimeOnly? BreakEnd);

/// <summary>How a weekday gets its hours. CK_LocationOperationalDay_DayType.</summary>
public static class CalendarDayType
{
    /// <summary>Inherit the standard window, as it stands now.</summary>
    public const string Standard = "Standard";

    /// <summary>This day has its own hours.</summary>
    public const string Custom = "Custom";

    /// <summary>All day. A branch that runs a night shift on Wednesdays.</summary>
    public const string TwentyFourHour = "TwentyFourHour";

    public static readonly string[] Allowed = [Standard, Custom, TwentyFourHour];
}
