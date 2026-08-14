using AMS.Modules.ServiceLevel.Calendar;
using AMS.Modules.ServiceLevel.Domain;
using AMS.Modules.ServiceLevel.Features.CreateHoliday;
using AMS.Modules.ServiceLevel.Features.GetLocationCalendar;
using AMS.Modules.ServiceLevel.Features.SearchHolidays;
using AMS.Modules.ServiceLevel.Features.SetHolidayLocations;
using AMS.Modules.ServiceLevel.Features.SetLocationCalendar;
using AMS.Modules.ServiceLevel.Features.UpdateHoliday;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Tests;

/// <summary>
/// The setup screens: Operational Hours and the Holiday Calendar, against a
/// real database. Pass one of two.
/// </summary>
[Collection(nameof(ServiceLevelCollectionDefinition))]
public sealed class CalendarSliceTests(ServiceLevelFixture fixture)
{
    // ------------------------------------------------------- the week

    [Fact]
    public async Task An_unconfigured_branch_reads_as_monday_to_friday_and_says_so()
    {
        // The arithmetic answers with the default week. The setup screen needs
        // to know it is a fallback and not a decision somebody made.
        await fixture.ResetAsync();

        var calendar = (await GetCalendarAsync(1)).Value;

        calendar.IsConfigured.ShouldBeFalse();
        calendar.StandardStartTime.ShouldBe(new TimeOnly(9, 0));
        calendar.Days.Count(d => d.IsWorkingDay).ShouldBe(5);
    }

    [Fact]
    public async Task A_week_can_be_set_and_read_back()
    {
        await fixture.ResetAsync();

        var set = await SetCalendarAsync(1, start: new TimeOnly(8, 30), end: new TimeOnly(17, 30));

        set.IsSuccess.ShouldBeTrue();
        var calendar = (await GetCalendarAsync(1)).Value;
        calendar.IsConfigured.ShouldBeTrue();
        calendar.StandardStartTime.ShouldBe(new TimeOnly(8, 30));
        calendar.Days.Single(d => d.DayOfWeek == 1).StartTime.ShouldBe(new TimeOnly(8, 30));
    }

    [Fact]
    public async Task Setting_a_week_twice_edits_rather_than_collides()
    {
        // UX_LocationOperationalHour_Location allows one calendar per branch,
        // so create and edit are the same act from the screen's point of view.
        await fixture.ResetAsync();
        await SetCalendarAsync(1, start: new TimeOnly(9, 0));

        var second = await SetCalendarAsync(1, start: new TimeOnly(10, 0));

        second.IsSuccess.ShouldBeTrue();
        (await GetCalendarAsync(1)).Value.StandardStartTime.ShouldBe(new TimeOnly(10, 0));
    }

    [Fact]
    public async Task Editing_the_standard_window_moves_every_standard_day_with_it()
    {
        // The reason a Standard day stores no times.
        await fixture.ResetAsync();
        await SetCalendarAsync(1, start: new TimeOnly(9, 0), end: new TimeOnly(18, 0));

        await SetCalendarAsync(1, start: new TimeOnly(7, 0), end: new TimeOnly(15, 0));

        var monday = (await GetCalendarAsync(1)).Value.Days.Single(d => d.DayOfWeek == 1);
        monday.StartTime.ShouldBe(new TimeOnly(7, 0));
        monday.EndTime.ShouldBe(new TimeOnly(15, 0));
    }

    [Fact]
    public async Task A_custom_day_keeps_its_own_hours_when_the_standard_window_moves()
    {
        await fixture.ResetAsync();
        var days = StandardWeek();
        days[3] = days[3] with
        {
            DayType = CalendarDayType.Custom,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(14, 0),
        };
        await SetCalendarAsync(1, days: days);

        await SetCalendarAsync(1, start: new TimeOnly(7, 0), end: new TimeOnly(15, 0), days: days);

        var wednesday = (await GetCalendarAsync(1)).Value.Days.Single(d => d.DayOfWeek == 3);
        wednesday.StartTime.ShouldBe(new TimeOnly(10, 0));
    }

    [Fact]
    public async Task A_branch_that_does_not_exist_has_no_calendar_to_set()
    {
        await fixture.ResetAsync();

        (await SetCalendarAsync(987654)).Error!.Code.ShouldBe("Location.NotFound");
    }

    [Fact]
    public async Task A_branch_must_close_after_it_opens()
    {
        await fixture.ResetAsync();

        (await SetCalendarAsync(1, start: new TimeOnly(18, 0), end: new TimeOnly(9, 0)))
            .Error!.Code.ShouldBe("LocationCalendar.Window");
    }

    [Fact]
    public async Task A_round_the_clock_branch_need_not_invent_opening_times()
    {
        // R2-9. The window CHECK is relaxed when the flag is set, and so is
        // this.
        await fixture.ResetAsync();

        var result = await SetCalendarAsync(
            1, roundTheClock: true, start: new TimeOnly(0, 0), end: new TimeOnly(0, 0));

        result.IsSuccess.ShouldBeTrue();
        (await GetCalendarAsync(1)).Value.IsRoundTheClock.ShouldBeTrue();
    }

    [Fact]
    public async Task A_break_needs_both_ends_and_must_fall_inside_the_window()
    {
        // A break outside the working window silently removes nothing, which
        // looks exactly like the configuration having worked.
        await fixture.ResetAsync();

        (await SetCalendarAsync(1, breakStart: new TimeOnly(13, 0)))
            .Error!.Code.ShouldBe("LocationCalendar.BreakPair");

        (await SetCalendarAsync(
            1, breakStart: new TimeOnly(19, 0), breakEnd: new TimeOnly(20, 0)))
            .Error!.Code.ShouldBe("LocationCalendar.BreakOutsideWindow");

        (await SetCalendarAsync(
            1, breakStart: new TimeOnly(14, 0), breakEnd: new TimeOnly(13, 0)))
            .Error!.Code.ShouldBe("LocationCalendar.BreakOrder");
    }

    [Fact]
    public async Task A_week_arrives_whole_or_not_at_all()
    {
        // Six rows would leave the seventh to a fallback nobody chose, and that
        // is only noticed when a Wednesday ticket is late.
        await fixture.ResetAsync();

        var result = await SetCalendarAsync(1, days: [.. StandardWeek().Take(6)]);

        result.Error!.Code.ShouldBe("LocationCalendar.SevenDays");
    }

    [Fact]
    public async Task A_weekday_may_appear_once()
    {
        await fixture.ResetAsync();
        var days = StandardWeek();
        days[6] = days[6] with { DayOfWeek = 0 };

        (await SetCalendarAsync(1, days: days)).Error!.Code
            .ShouldBe("LocationCalendar.DuplicateDay");
    }

    [Fact]
    public async Task A_custom_day_needs_its_own_times()
    {
        await fixture.ResetAsync();
        var days = StandardWeek();
        days[3] = days[3] with { DayType = CalendarDayType.Custom };

        (await SetCalendarAsync(1, days: days)).Error!.Code
            .ShouldBe("LocationCalendar.CustomTimes");
    }

    [Fact]
    public async Task Working_saturdays_are_stored_as_five_rows()
    {
        // All five, not just the working ones: "we work the first and third"
        // and "we have not decided" are different answers.
        await fixture.ResetAsync();

        await SetCalendarAsync(1, workingSaturdays: [1, 3]);

        await using var db = fixture.NewContext();
        var rules = await db.LocationSaturdayRules
            .OrderBy(s => s.Occurrence)
            .ToListAsync(TestContext.Current.CancellationToken);
        rules.Count.ShouldBe(5);
        rules.Where(r => r.IsWorking).Select(r => (int)r.Occurrence).ShouldBe([1, 3]);
    }

    [Fact]
    public async Task No_working_saturdays_stores_no_rules_at_all()
    {
        // Five "not working" rows would close a branch that had simply not
        // answered the question.
        await fixture.ResetAsync();

        await SetCalendarAsync(1, workingSaturdays: []);

        await using var db = fixture.NewContext();
        (await db.LocationSaturdayRules.CountAsync(TestContext.Current.CancellationToken))
            .ShouldBe(0);
    }

    [Fact]
    public async Task A_saturday_occurrence_is_one_through_five()
    {
        await fixture.ResetAsync();

        (await SetCalendarAsync(1, workingSaturdays: [1, 6])).Error!.Code
            .ShouldBe("LocationCalendar.SaturdayOccurrence");
    }

    [Fact]
    public async Task The_stored_week_drives_the_arithmetic()
    {
        // The point of the whole slice: what the setup screen saves is what the
        // SLA clock reads.
        await fixture.ResetAsync();
        await SetCalendarAsync(1, start: new TimeOnly(10, 0), end: new TimeOnly(16, 0));

        var loader = NewLoader();
        var calendar = await loader.LoadAsync(1, TestContext.Current.CancellationToken);

        OperationalCalendar.IsOperational(calendar, Ist(2026, 8, 3, 9, 30)).ShouldBeFalse();
        OperationalCalendar.IsOperational(calendar, Ist(2026, 8, 3, 10, 30)).ShouldBeTrue();
    }

    [Fact]
    public async Task A_branch_keeps_its_own_time_zone()
    {
        await fixture.ResetAsync();
        await SetCalendarAsync(1);
        await SetCalendarAsync(3);

        var loader = NewLoader();
        var india = await loader.LoadAsync(1, TestContext.Current.CancellationToken);
        var london = await loader.LoadAsync(3, TestContext.Current.CancellationToken);

        // 04:00 UTC is 09:30 in India and 05:00 in London.
        var instant = new DateTime(2026, 8, 3, 4, 0, 0, DateTimeKind.Utc);
        OperationalCalendar.IsOperational(india, instant).ShouldBeTrue();
        OperationalCalendar.IsOperational(london, instant).ShouldBeFalse();
    }

    // --------------------------------------------------------- holidays

    [Fact]
    public async Task A_holiday_for_everybody_needs_no_branches()
    {
        await fixture.ResetAsync();

        var created = await CreateHolidayAsync("Republic Day", new DateOnly(2026, 1, 26), forAll: true);

        created.IsSuccess.ShouldBeTrue();
        created.Value.LocationCount.ShouldBe(0);
        (await SearchHolidaysAsync()).Value.Rows.Single().AppliesToAllLocations.ShouldBeTrue();
    }

    [Fact]
    public async Task A_regional_holiday_attached_to_nothing_is_refused()
    {
        // Observed nowhere, and it looks exactly like it working. The stored
        // AppliesToAllLocations flag exists so the two cannot be confused.
        await fixture.ResetAsync();

        var result = await CreateHolidayAsync("Regional day", new DateOnly(2026, 4, 14));

        result.Error!.Code.ShouldBe("Holiday.NoLocations");
    }

    [Fact]
    public async Task The_year_is_taken_from_the_date_not_from_the_client()
    {
        // CK_HolidayCalendar_YearMatchesDate requires them to agree; a client
        // that could send both could send two that disagree.
        await fixture.ResetAsync();

        await CreateHolidayAsync("Diwali", new DateOnly(2026, 11, 8), forAll: true);

        (await SearchHolidaysAsync()).Value.Rows.Single().HolidayYear.ShouldBe(2026);
    }

    [Fact]
    public async Task A_recurring_holiday_takes_its_month_and_day_from_the_date()
    {
        await fixture.ResetAsync();

        await CreateHolidayAsync(
            "Republic Day", new DateOnly(2026, 1, 26), forAll: true, recurring: true);

        var row = (await SearchHolidaysAsync()).Value.Rows.Single();
        row.RecurrenceMonth.ShouldBe((byte)1);
        row.RecurrenceDay.ShouldBe((byte)26);
    }

    [Fact]
    public async Task A_holiday_type_the_database_does_not_allow_is_refused()
    {
        await fixture.ResetAsync();

        (await CreateHolidayAsync("Odd", new DateOnly(2026, 5, 1), forAll: true, type: "Bank"))
            .Error!.Code.ShouldBe("Holiday.UnknownType");
    }

    [Fact]
    public async Task A_holiday_outside_the_allowed_years_is_refused()
    {
        await fixture.ResetAsync();

        (await CreateHolidayAsync("Ancient", new DateOnly(1999, 1, 1), forAll: true))
            .Error!.Code.ShouldBe("Holiday.Year");
    }

    [Fact]
    public async Task Searching_by_branch_includes_the_holidays_that_apply_everywhere()
    {
        // A screen that showed only the attached ones would tell a branch
        // manager their branch works on Republic Day.
        await fixture.ResetAsync();
        await CreateHolidayAsync("Republic Day", new DateOnly(2026, 1, 26), forAll: true);
        await CreateHolidayAsync("Local festival", new DateOnly(2026, 4, 14), locationIds: [2]);

        var forBranchTwo = (await SearchHolidaysAsync(locationId: 2)).Value.Rows;
        var forBranchOne = (await SearchHolidaysAsync(locationId: 1)).Value.Rows;

        forBranchTwo.Count.ShouldBe(2);
        forBranchOne.Single().HolidayName.ShouldBe("Republic Day");
    }

    [Fact]
    public async Task Searching_by_year_still_finds_the_recurring_ones()
    {
        // A recurring holiday belongs to every year, not to the one it happened
        // to be entered in.
        await fixture.ResetAsync();
        await CreateHolidayAsync(
            "Republic Day", new DateOnly(2026, 1, 26), forAll: true, recurring: true);

        (await SearchHolidaysAsync(year: 2031)).Value.Rows.Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_holiday_can_be_edited_and_retired()
    {
        await fixture.ResetAsync();
        var id = (await CreateHolidayAsync("Typo", new DateOnly(2026, 5, 1), forAll: true)).Value.Id;

        var updated = await UpdateHolidayAsync(id, "Labour Day", new DateOnly(2026, 5, 1), isActive: false);

        updated.Value.HolidayName.ShouldBe("Labour Day");
        (await SearchHolidaysAsync()).Value.Rows.ShouldBeEmpty();
        (await SearchHolidaysAsync(activeOnly: false)).Value.Rows.Single().IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task An_unknown_holiday_cannot_be_edited_or_attached()
    {
        await fixture.ResetAsync();

        (await UpdateHolidayAsync(987654, "Ghost", new DateOnly(2026, 5, 1))).Error!.Code
            .ShouldBe("Holiday.NotFound");
        (await SetLocationsAsync(987654, [1])).Error!.Code.ShouldBe("Holiday.NotFound");
    }

    [Fact]
    public async Task The_branches_that_observe_a_holiday_are_set_as_a_whole()
    {
        await fixture.ResetAsync();
        var id = (await CreateHolidayAsync(
            "Local festival", new DateOnly(2026, 4, 14), locationIds: [1, 2])).Value.Id;

        var set = await SetLocationsAsync(id, [2, 3]);

        set.Value.LocationCount.ShouldBe(2);
        (await SearchHolidaysAsync()).Value.Rows.Single().LocationIds.ShouldBe([2, 3]);
    }

    [Fact]
    public async Task A_branch_that_stays_attached_keeps_the_date_it_started_observing()
    {
        await fixture.ResetAsync();
        var id = (await CreateHolidayAsync(
            "Local festival", new DateOnly(2026, 4, 14), locationIds: [1, 2])).Value.Id;

        var original = await ObservedSinceAsync(id, locationId: 1);

        fixture.Clock.Advance(TimeSpan.FromDays(30));
        await SetLocationsAsync(id, [1, 3]);

        (await ObservedSinceAsync(id, locationId: 1)).ShouldBe(original);
    }

    [Fact]
    public async Task A_regional_holiday_cannot_have_its_last_branch_taken_away()
    {
        await fixture.ResetAsync();
        var id = (await CreateHolidayAsync(
            "Local festival", new DateOnly(2026, 4, 14), locationIds: [1])).Value.Id;

        (await SetLocationsAsync(id, [])).Error!.Code.ShouldBe("Holiday.NoLocations");
    }

    [Fact]
    public async Task A_stored_holiday_closes_the_branch_in_the_arithmetic()
    {
        // The other end of the same point: what the holiday screen saves is
        // what the SLA clock reads.
        await fixture.ResetAsync();
        await CreateHolidayAsync("Local festival", new DateOnly(2026, 8, 5), locationIds: [2]);

        var loader = NewLoader();
        var branchTwo = await loader.LoadAsync(2, TestContext.Current.CancellationToken);
        var branchOne = await loader.LoadAsync(1, TestContext.Current.CancellationToken);

        OperationalCalendar.IsWorkingDay(branchTwo, new DateOnly(2026, 8, 5)).ShouldBeFalse();
        OperationalCalendar.IsWorkingDay(branchOne, new DateOnly(2026, 8, 5)).ShouldBeTrue();
    }

    [Fact]
    public async Task An_unconfigured_branch_still_observes_its_holidays()
    {
        // The default week is about HOURS, not about pretending Republic Day is
        // a working day.
        await fixture.ResetAsync();
        await CreateHolidayAsync("Republic Day", new DateOnly(2026, 1, 26), forAll: true);

        var calendar = await NewLoader().LoadAsync(1, TestContext.Current.CancellationToken);

        OperationalCalendar.IsWorkingDay(calendar, new DateOnly(2026, 1, 26)).ShouldBeFalse();
    }

    [Fact]
    public async Task A_retired_holiday_stops_closing_the_branch()
    {
        await fixture.ResetAsync();
        var id = (await CreateHolidayAsync(
            "Cancelled", new DateOnly(2026, 8, 5), forAll: true)).Value.Id;
        await UpdateHolidayAsync(id, "Cancelled", new DateOnly(2026, 8, 5), forAll: true, isActive: false);

        var calendar = await NewLoader().LoadAsync(1, TestContext.Current.CancellationToken);

        OperationalCalendar.IsWorkingDay(calendar, new DateOnly(2026, 8, 5)).ShouldBeTrue();
    }

    // --------------------------------------------------------------- plumbing

    private static readonly TimeZoneInfo India =
        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    private static DateTime Ist(int year, int month, int day, int hour, int minute) =>
        TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified), India);

    private CalendarLoader NewLoader() => new(fixture.NewContext(), fixture.Locations);

    private async Task<DateTime> ObservedSinceAsync(int holidayId, int locationId)
    {
        await using var db = fixture.NewContext();

        return await db.HolidayLocations
            .Where(l => l.HolidayCalendarId == holidayId && l.LocationId == locationId)
            .Select(l => l.CreatedOnUtc)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private static List<SetLocationCalendarCommand.Day> StandardWeek() =>
        [.. Enumerable.Range(0, 7).Select(day => new SetLocationCalendarCommand.Day(
            (byte)day,
            day is >= 1 and <= 5,
            CalendarDayType.Standard,
            null, null, null, null))];

    private Task<Result<GetLocationCalendarResponse>> GetCalendarAsync(int locationId)
    {
        var handler = new GetLocationCalendarHandler(NewLoader());

        return handler.HandleAsync(
            new GetLocationCalendarQuery(locationId), TestContext.Current.CancellationToken);
    }

    private Task<Result<SetLocationCalendarResponse>> SetCalendarAsync(
        int locationId,
        bool roundTheClock = false,
        TimeOnly? start = null,
        TimeOnly? end = null,
        TimeOnly? breakStart = null,
        TimeOnly? breakEnd = null,
        int deferFinalMinutes = 0,
        bool deferOnFriday = false,
        IReadOnlyList<SetLocationCalendarCommand.Day>? days = null,
        IReadOnlyList<int>? workingSaturdays = null)
    {
        var handler = new SetLocationCalendarHandler(
            fixture.NewContext(), fixture.Locations, NewLoader(), fixture.Clock,
            fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new SetLocationCalendarCommand(
                locationId,
                roundTheClock,
                start ?? new TimeOnly(9, 0),
                end ?? new TimeOnly(18, 0),
                breakStart,
                breakEnd,
                deferFinalMinutes,
                deferOnFriday,
                days ?? StandardWeek(),
                workingSaturdays ?? []),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<CreateHolidayResponse>> CreateHolidayAsync(
        string name,
        DateOnly date,
        bool forAll = false,
        bool recurring = false,
        string type = HolidayType.Government,
        IReadOnlyList<int>? locationIds = null)
    {
        var handler = new CreateHolidayHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new CreateHolidayCommand(name, date, type, forAll, recurring, null, locationIds ?? []),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<UpdateHolidayResponse>> UpdateHolidayAsync(
        int id,
        string name,
        DateOnly date,
        bool forAll = true,
        bool isActive = true)
    {
        var handler = new UpdateHolidayHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new UpdateHolidayCommand(
                id, name, date, HolidayType.Government, forAll, false, null, isActive),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SetHolidayLocationsResponse>> SetLocationsAsync(
        int id, IReadOnlyList<int> locationIds)
    {
        var handler = new SetHolidayLocationsHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new SetHolidayLocationsCommand(id, locationIds),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchHolidaysResponse>> SearchHolidaysAsync(
        int? year = null, int? locationId = null, bool activeOnly = true)
    {
        var handler = new SearchHolidaysHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchHolidaysQuery(year, null, locationId, activeOnly),
            TestContext.Current.CancellationToken);
    }
}
