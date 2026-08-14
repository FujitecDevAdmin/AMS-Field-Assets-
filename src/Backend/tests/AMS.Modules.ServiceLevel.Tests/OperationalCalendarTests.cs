using AMS.Modules.ServiceLevel.Calendar;

namespace AMS.Modules.ServiceLevel.Tests;

/// <summary>
/// The arithmetic the whole module exists for. No database, no clock: state a
/// working week, ask a question, check the answer.
/// </summary>
/// <remarks>
/// Every date in here is real and deliberate. August 2026 is used throughout:
/// the 3rd is a Monday, the 1st a Saturday, the 29th the fifth Saturday.
/// </remarks>
public sealed class OperationalCalendarTests
{
    private static readonly TimeZoneInfo India =
        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    /// <summary>Local 09:00 on a Monday, as an instant.</summary>
    private static DateTime MondayMorning => Utc(2026, 8, 3, 9, 0);

    // ---------------------------------------------------------- the window

    [Fact]
    public void A_branch_with_no_configuration_works_monday_to_friday_nine_to_six()
    {
        // It still raises tickets. A calendar answering "never operational"
        // would make every one of them instantly overdue.
        var calendar = Default();

        OperationalCalendar.IsWorkingDay(calendar, new DateOnly(2026, 8, 3)).ShouldBeTrue();
        OperationalCalendar.IsWorkingDay(calendar, new DateOnly(2026, 8, 8)).ShouldBeFalse();
        OperationalCalendar.IsOperational(calendar, MondayMorning).ShouldBeTrue();
        OperationalCalendar.IsOperational(calendar, Utc(2026, 8, 3, 8, 59)).ShouldBeFalse();
    }

    [Fact]
    public void Closing_time_is_not_operational()
    {
        // The window is half-open. 18:00 is when the branch shuts, not the last
        // minute it is open, and a minute counted at both ends is a minute
        // counted twice.
        var calendar = Default();

        OperationalCalendar.IsOperational(calendar, Utc(2026, 8, 3, 17, 59)).ShouldBeTrue();
        OperationalCalendar.IsOperational(calendar, Utc(2026, 8, 3, 18, 0)).ShouldBeFalse();
    }

    [Fact]
    public void A_break_splits_the_day_into_two_windows()
    {
        var calendar = Default() with
        {
            BreakStart = new TimeOnly(13, 0),
            BreakEnd = new TimeOnly(14, 0),
        };

        var windows = OperationalCalendar.Windows(calendar, new DateOnly(2026, 8, 3));

        windows.Count.ShouldBe(2);
        OperationalCalendar.IsOperational(calendar, Utc(2026, 8, 3, 13, 30)).ShouldBeFalse();
        OperationalCalendar.IsOperational(calendar, Utc(2026, 8, 3, 14, 0)).ShouldBeTrue();
    }

    [Fact]
    public void A_round_the_clock_branch_is_always_open()
    {
        var calendar = Default() with { IsRoundTheClock = true };

        OperationalCalendar.IsOperational(calendar, Utc(2026, 8, 3, 3, 0)).ShouldBeTrue();
        OperationalCalendar
            .OperationalMinutesBetween(calendar, Utc(2026, 8, 3, 0, 0), Utc(2026, 8, 4, 0, 0))
            .ShouldBe(1440);
    }

    [Fact]
    public void A_custom_day_keeps_its_own_hours()
    {
        var calendar = Default();
        calendar = calendar with
        {
            Days = [.. calendar.Days.Select(d => d.DayOfWeek == DayOfWeek.Wednesday
                ? d with
                {
                    DayType = CalendarDayType.Custom,
                    Start = new TimeOnly(10, 0),
                    End = new TimeOnly(14, 0),
                }
                : d)],
        };

        OperationalCalendar.IsOperational(calendar, Utc(2026, 8, 5, 9, 30)).ShouldBeFalse();
        OperationalCalendar.IsOperational(calendar, Utc(2026, 8, 5, 10, 30)).ShouldBeTrue();
        OperationalCalendar.IsOperational(calendar, Utc(2026, 8, 5, 15, 0)).ShouldBeFalse();
    }

    [Fact]
    public void A_standard_day_follows_the_standard_window_as_it_now_stands()
    {
        // This is why a Standard day stores no times: the seven copies would go
        // stale the first time somebody edited the standard window.
        var calendar = Default() with
        {
            StandardStart = new TimeOnly(8, 0),
            StandardEnd = new TimeOnly(16, 0),
        };

        OperationalCalendar.IsOperational(calendar, Utc(2026, 8, 3, 8, 30)).ShouldBeTrue();
        OperationalCalendar.IsOperational(calendar, Utc(2026, 8, 3, 17, 0)).ShouldBeFalse();
    }

    // -------------------------------------------------------- the Saturday

    [Fact]
    public void A_saturday_must_satisfy_both_the_weekday_row_and_its_occurrence()
    {
        // "We work Saturdays" and "we work the first and third" are different
        // statements, and a branch makes both. This is why the Saturday rules
        // cannot collapse into the weekday table.
        var calendar = WorkingSaturdays(1, 3);

        // 1 August 2026 is the first Saturday; the 8th is the second.
        OperationalCalendar.IsWorkingDay(calendar, new DateOnly(2026, 8, 1)).ShouldBeTrue();
        OperationalCalendar.IsWorkingDay(calendar, new DateOnly(2026, 8, 8)).ShouldBeFalse();
        OperationalCalendar.IsWorkingDay(calendar, new DateOnly(2026, 8, 15)).ShouldBeTrue();
    }

    [Fact]
    public void A_saturday_the_weekday_row_closes_stays_closed_whatever_the_occurrence_says()
    {
        var calendar = WorkingSaturdays(1, 3);
        calendar = calendar with
        {
            Days = [.. calendar.Days.Select(d => d.DayOfWeek == DayOfWeek.Saturday
                ? d with { IsWorkingDay = false }
                : d)],
        };

        OperationalCalendar.IsWorkingDay(calendar, new DateOnly(2026, 8, 1)).ShouldBeFalse();
    }

    [Fact]
    public void No_saturday_rules_at_all_means_every_saturday_follows_the_weekday_row()
    {
        // A branch that has simply not answered the question is not a branch
        // that has said no.
        var calendar = WorkingSaturdays();

        OperationalCalendar.IsWorkingDay(calendar, new DateOnly(2026, 8, 8)).ShouldBeTrue();
        OperationalCalendar.IsWorkingDay(calendar, new DateOnly(2026, 8, 29)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(7, 1)]
    [InlineData(8, 2)]
    [InlineData(29, 5)]
    public void The_occurrence_is_counted_from_the_first_of_the_month(int day, int expected) =>
        OperationalCalendar.OccurrenceInMonth(new DateOnly(2026, 8, day)).ShouldBe(expected);

    // --------------------------------------------------------- the holiday

    [Fact]
    public void A_holiday_closes_the_branch()
    {
        var calendar = Default() with
        {
            FixedHolidays = new HashSet<DateOnly> { new(2026, 8, 5) },
        };

        OperationalCalendar.IsWorkingDay(calendar, new DateOnly(2026, 8, 5)).ShouldBeFalse();
        OperationalCalendar.IsWorkingDay(calendar, new DateOnly(2026, 8, 6)).ShouldBeTrue();
    }

    [Fact]
    public void A_recurring_holiday_does_not_need_re_entering_every_year()
    {
        // Republic Day, matched on month and day.
        var calendar = Default() with
        {
            RecurringHolidays = new HashSet<(int, int)> { (1, 26) },
        };

        OperationalCalendar.IsHoliday(calendar, new DateOnly(2026, 1, 26)).ShouldBeTrue();
        OperationalCalendar.IsHoliday(calendar, new DateOnly(2031, 1, 26)).ShouldBeTrue();
    }

    [Fact]
    public void A_twenty_ninth_of_february_recurrence_is_observed_on_the_twenty_eighth()
    {
        // The design script states this as an application rule precisely so
        // nobody hunts for a missing row.
        var calendar = Default() with
        {
            RecurringHolidays = new HashSet<(int, int)> { (2, 29) },
        };

        OperationalCalendar.IsHoliday(calendar, new DateOnly(2028, 2, 29)).ShouldBeTrue();
        OperationalCalendar.IsHoliday(calendar, new DateOnly(2026, 2, 28)).ShouldBeTrue();
        OperationalCalendar.IsHoliday(calendar, new DateOnly(2028, 2, 28)).ShouldBeFalse();
    }

    // ------------------------------------------------------ counting minutes

    [Fact]
    public void Minutes_are_counted_only_inside_the_window()
    {
        // 08:00 to 20:00 on a nine-to-six Monday is nine hours, not twelve.
        var minutes = OperationalCalendar.OperationalMinutesBetween(
            Default(), Utc(2026, 8, 3, 8, 0), Utc(2026, 8, 3, 20, 0));

        minutes.ShouldBe(9 * 60);
    }

    [Fact]
    public void A_span_over_a_weekend_consumes_nothing_for_the_weekend()
    {
        // Friday 17:00 to Monday 10:00: one hour on Friday, one on Monday.
        var minutes = OperationalCalendar.OperationalMinutesBetween(
            Default(), Utc(2026, 8, 7, 17, 0), Utc(2026, 8, 10, 10, 0));

        minutes.ShouldBe(120);
    }

    [Fact]
    public void A_break_is_not_counted()
    {
        var calendar = Default() with
        {
            BreakStart = new TimeOnly(13, 0),
            BreakEnd = new TimeOnly(14, 0),
        };

        OperationalCalendar
            .OperationalMinutesBetween(calendar, Utc(2026, 8, 3, 9, 0), Utc(2026, 8, 3, 18, 0))
            .ShouldBe(8 * 60);
    }

    [Fact]
    public void A_span_that_runs_backwards_is_zero_not_negative()
    {
        OperationalCalendar
            .OperationalMinutesBetween(Default(), Utc(2026, 8, 3, 12, 0), Utc(2026, 8, 3, 9, 0))
            .ShouldBe(0);
    }

    [Fact]
    public void A_span_entirely_outside_the_window_is_zero()
    {
        OperationalCalendar
            .OperationalMinutesBetween(Default(), Utc(2026, 8, 3, 19, 0), Utc(2026, 8, 3, 22, 0))
            .ShouldBe(0);
    }

    // -------------------------------------------------------- adding minutes

    [Fact]
    public void Four_working_hours_from_monday_morning_is_monday_afternoon()
    {
        var due = OperationalCalendar.AddOperationalMinutes(Default(), MondayMorning, 4 * 60);

        due.ShouldBe(Utc(2026, 8, 3, 13, 0));
    }

    [Fact]
    public void Adding_minutes_rolls_over_closing_time_into_the_next_working_day()
    {
        // Two hours from 17:00 on Monday: one hour today, one tomorrow morning.
        var due = OperationalCalendar.AddOperationalMinutes(
            Default(), Utc(2026, 8, 3, 17, 0), 120);

        due.ShouldBe(Utc(2026, 8, 4, 10, 0));
    }

    [Fact]
    public void Adding_minutes_steps_over_a_weekend()
    {
        // Two hours from Friday 17:00 lands on Monday morning.
        var due = OperationalCalendar.AddOperationalMinutes(
            Default(), Utc(2026, 8, 7, 17, 0), 120);

        due.ShouldBe(Utc(2026, 8, 10, 10, 0));
    }

    [Fact]
    public void Adding_minutes_steps_over_a_holiday()
    {
        var calendar = Default() with
        {
            FixedHolidays = new HashSet<DateOnly> { new(2026, 8, 4) },
        };

        var due = OperationalCalendar.AddOperationalMinutes(
            calendar, Utc(2026, 8, 3, 17, 0), 120);

        due.ShouldBe(Utc(2026, 8, 5, 10, 0));
    }

    [Fact]
    public void A_target_landing_exactly_on_the_bell_is_that_moment_not_the_next_morning()
    {
        // Nine hours from 09:00 is 18:00. A target that lands on the closing
        // time has been met, not missed by a night.
        var due = OperationalCalendar.AddOperationalMinutes(Default(), MondayMorning, 9 * 60);

        due.ShouldBe(Utc(2026, 8, 3, 18, 0));
    }

    [Fact]
    public void Adding_minutes_from_outside_the_window_starts_at_the_next_opening()
    {
        // 06:00 on a Monday: the clock has not started yet.
        var due = OperationalCalendar.AddOperationalMinutes(
            Default(), Utc(2026, 8, 3, 6, 0), 60);

        due.ShouldBe(Utc(2026, 8, 3, 10, 0));
    }

    [Fact]
    public void Adding_nothing_gives_the_next_moment_the_branch_is_open()
    {
        OperationalCalendar
            .AddOperationalMinutes(Default(), Utc(2026, 8, 8, 12, 0), 0)
            .ShouldBe(Utc(2026, 8, 10, 9, 0));
    }

    [Fact]
    public void A_branch_that_never_opens_gives_no_answer_rather_than_spinning()
    {
        // A configuration mistake, not a branch shut for ever - but the walker
        // cannot tell, so it is bounded.
        var calendar = Default();
        calendar = calendar with
        {
            Days = [.. calendar.Days.Select(d => d with { IsWorkingDay = false })],
        };

        OperationalCalendar.AddOperationalMinutes(calendar, MondayMorning, 60).ShouldBeNull();
        OperationalCalendar.NextOperationalStart(calendar, MondayMorning).ShouldBeNull();
    }

    // -------------------------------------------------------- intake rules

    [Fact]
    public void A_ticket_raised_while_the_branch_is_open_starts_straight_away()
    {
        OperationalCalendar
            .NextOperationalStart(Default(), Utc(2026, 8, 3, 11, 0))
            .ShouldBe(Utc(2026, 8, 3, 11, 0));
    }

    [Fact]
    public void A_ticket_raised_after_hours_starts_the_next_morning()
    {
        OperationalCalendar
            .NextOperationalStart(Default(), Utc(2026, 8, 3, 21, 0))
            .ShouldBe(Utc(2026, 8, 4, 9, 0));
    }

    [Fact]
    public void A_ticket_raised_in_the_final_minutes_starts_tomorrow()
    {
        // The handbook's rule, and configuration rather than code: a branch
        // manager has to be able to turn it off.
        var calendar = Default() with { DeferFinalMinutes = 30 };

        OperationalCalendar
            .NextOperationalStart(calendar, Utc(2026, 8, 3, 17, 45))
            .ShouldBe(Utc(2026, 8, 4, 9, 0));
    }

    [Fact]
    public void The_final_minutes_rule_leaves_earlier_tickets_alone()
    {
        var calendar = Default() with { DeferFinalMinutes = 30 };

        OperationalCalendar
            .NextOperationalStart(calendar, Utc(2026, 8, 3, 17, 15))
            .ShouldBe(Utc(2026, 8, 3, 17, 15));
    }

    [Fact]
    public void A_ticket_raised_on_a_friday_can_be_held_to_monday()
    {
        var calendar = Default() with { DeferNewTicketsOnFriday = true };

        OperationalCalendar
            .NextOperationalStart(calendar, Utc(2026, 8, 7, 10, 0))
            .ShouldBe(Utc(2026, 8, 10, 9, 0));
    }

    [Fact]
    public void The_deferral_rules_can_be_ignored()
    {
        // A Critical policy usually does: a production outage does not wait for
        // Monday.
        var calendar = Default() with
        {
            DeferNewTicketsOnFriday = true,
            DeferFinalMinutes = 60,
        };

        OperationalCalendar
            .NextOperationalStart(calendar, Utc(2026, 8, 7, 10, 0), applyDeferralRules: false)
            .ShouldBe(Utc(2026, 8, 7, 10, 0));
    }

    // ------------------------------------------------------------ time zone

    [Fact]
    public void The_window_is_local_to_the_branch_not_to_the_server()
    {
        // 09:00 IST is 03:30 UTC. A branch opens where it stands.
        var calendar = Default();

        OperationalCalendar.IsOperational(calendar, new DateTime(2026, 8, 3, 3, 30, 0, DateTimeKind.Utc))
            .ShouldBeTrue();
        OperationalCalendar.IsOperational(calendar, new DateTime(2026, 8, 3, 3, 0, 0, DateTimeKind.Utc))
            .ShouldBeFalse();
    }

    [Fact]
    public void Two_branches_in_different_zones_are_open_at_different_instants()
    {
        var india = Default();
        var london = Default() with { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time") };

        // 09:30 UTC: mid-morning in London, mid-afternoon in India, both open.
        var instant = new DateTime(2026, 8, 3, 9, 30, 0, DateTimeKind.Utc);
        OperationalCalendar.IsOperational(india, instant).ShouldBeTrue();
        OperationalCalendar.IsOperational(london, instant).ShouldBeTrue();

        // 04:00 UTC: 09:30 in India, 05:00 in London.
        var early = new DateTime(2026, 8, 3, 4, 0, 0, DateTimeKind.Utc);
        OperationalCalendar.IsOperational(india, early).ShouldBeTrue();
        OperationalCalendar.IsOperational(london, early).ShouldBeFalse();
    }

    [Fact]
    public void A_due_date_computed_across_a_daylight_saving_change_keeps_the_local_window()
    {
        // London's clocks go back on 25 October 2026. A branch that opens at
        // 09:00 opens at 09:00 on both sides of it; only the instant moves.
        var london = Default() with
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"),
        };

        // Friday 23 October, 17:00 local = 16:00 UTC (BST). Two hours takes it
        // to Monday 26 October at 10:00 local, which is 10:00 UTC (GMT).
        var due = OperationalCalendar.AddOperationalMinutes(
            london, new DateTime(2026, 10, 23, 16, 0, 0, DateTimeKind.Utc), 120);

        due.ShouldBe(new DateTime(2026, 10, 26, 10, 0, 0, DateTimeKind.Utc));
    }

    // --------------------------------------------------------------- plumbing

    private static CalendarSnapshot Default() => CalendarSnapshot.Default(1, India);

    private static CalendarSnapshot WorkingSaturdays(params int[] occurrences)
    {
        var calendar = Default();

        return calendar with
        {
            Days = [.. calendar.Days.Select(d => d.DayOfWeek == DayOfWeek.Saturday
                ? d with { IsWorkingDay = true }
                : d)],
            WorkingSaturdays = occurrences.ToHashSet(),
        };
    }

    /// <summary>An instant, stated as local Indian time.</summary>
    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified), India);
}
