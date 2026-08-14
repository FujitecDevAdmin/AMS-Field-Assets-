using AMS.Modules.ServiceLevel.Calendar;
using AMS.Modules.ServiceLevel.Domain;
using AMS.Modules.ServiceLevel.Features.CreateHoliday;
using AMS.Modules.ServiceLevel.Features.CreateSlaPolicy;
using AMS.Modules.ServiceLevel.Features.SearchEscalationLog;
using AMS.Modules.ServiceLevel.Features.SearchSlaPolicies;
using AMS.Modules.ServiceLevel.Features.SetLocationCalendar;
using AMS.Modules.ServiceLevel.Features.SetSlaEscalations;
using AMS.Modules.ServiceLevel.Features.UpdateSlaPolicy;
using AMS.Modules.ServiceLevel.PublicApi;
using AMS.Modules.ServiceLevel.PublicApi.ServiceLevel;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Tests;

/// <summary>
/// SLA policies, the escalation ladder, and the calculator ServiceDesk asks.
/// Pass two of two.
/// </summary>
[Collection(nameof(ServiceLevelCollectionDefinition))]
public sealed class SlaPolicyTests(ServiceLevelFixture fixture)
{
    // -------------------------------------------------------- the policy

    [Fact]
    public async Task A_policy_can_be_created_and_listed()
    {
        await fixture.ResetAsync();

        var created = await CreatePolicyAsync("Standard medium", SlaPriority.Medium, 60, 480);

        created.IsSuccess.ShouldBeTrue();
        var row = (await SearchPoliciesAsync()).Value.Rows.Single();
        row.ResponseTargetMinutes.ShouldBe(60);
        row.ResolutionTargetMinutes.ShouldBe(480);
    }

    [Fact]
    public async Task Only_one_active_policy_may_cover_a_priority()
    {
        // Two live "High" policies means a ticket gets whichever the query
        // ordered first. A filtered unique index, not a rule in code.
        await fixture.ResetAsync();
        await CreatePolicyAsync("First high", SlaPriority.High, 30, 240);

        var second = await CreatePolicyAsync("Second high", SlaPriority.High, 15, 120);

        second.Error!.Code.ShouldBe("SlaPolicy.PriorityTaken");
    }

    [Fact]
    public async Task Retiring_a_policy_frees_its_priority()
    {
        await fixture.ResetAsync();
        var id = (await CreatePolicyAsync("Old high", SlaPriority.High, 30, 240)).Value.Id;

        await UpdatePolicyAsync(id, "Old high", 30, 240, isActive: false);

        (await CreatePolicyAsync("New high", SlaPriority.High, 15, 120)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Two_policies_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 480);

        (await CreatePolicyAsync("Standard", SlaPriority.Low, 120, 960)).Error!.Code
            .ShouldBe("SlaPolicy.NameTaken");
    }

    [Fact]
    public async Task A_response_target_longer_than_the_resolution_target_is_refused()
    {
        // Always a typo, and it silently makes every ticket look compliant.
        await fixture.ResetAsync();

        (await CreatePolicyAsync("Backwards", SlaPriority.Medium, 480, 60)).Error!.Code
            .ShouldBe("SlaPolicy.ResponseBeyondResolution");
    }

    [Fact]
    public async Task A_priority_the_database_does_not_allow_is_refused()
    {
        await fixture.ResetAsync();

        (await CreatePolicyAsync("Odd", "Urgent", 60, 480)).Error!.Code
            .ShouldBe("SlaPolicy.UnknownPriority");
    }

    [Fact]
    public async Task Targets_must_be_more_than_nothing()
    {
        await fixture.ResetAsync();

        (await CreatePolicyAsync("Zero", SlaPriority.Medium, 0, 480)).Error!.Code
            .ShouldBe("SlaPolicy.Targets");
    }

    [Fact]
    public async Task An_unknown_policy_cannot_be_edited()
    {
        await fixture.ResetAsync();

        (await UpdatePolicyAsync(987654, "Ghost", 60, 480)).Error!.Code
            .ShouldBe("SlaPolicy.NotFound");
    }

    [Fact]
    public async Task Policies_are_listed_most_urgent_first()
    {
        // The screen is four rows a reader compares top to bottom.
        await fixture.ResetAsync();
        await CreatePolicyAsync("Low", SlaPriority.Low, 240, 2880);
        await CreatePolicyAsync("Critical", SlaPriority.Critical, 15, 60);
        await CreatePolicyAsync("Medium", SlaPriority.Medium, 60, 480);

        (await SearchPoliciesAsync()).Value.Rows.Select(r => r.Priority)
            .ShouldBe([SlaPriority.Critical, SlaPriority.Medium, SlaPriority.Low]);
    }

    // ------------------------------------------------------ the ladder

    [Fact]
    public async Task A_ladder_can_be_set_and_read_back()
    {
        await fixture.ResetAsync();
        var id = (await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 480)).Value.Id;

        var set = await SetEscalationsAsync(id,
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician),
            Rung(EscalationType.Resolution, 2, 150, EscalationRecipient.TeamLead));

        set.Value.ResolutionLevelCount.ShouldBe(2);
        (await SearchPoliciesAsync()).Value.Rows.Single().Escalations.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Setting_the_ladder_again_replaces_it()
    {
        await fixture.ResetAsync();
        var id = (await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 480)).Value.Id;
        await SetEscalationsAsync(id,
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician),
            Rung(EscalationType.Resolution, 2, 150, EscalationRecipient.TeamLead));

        var replaced = await SetEscalationsAsync(id,
            Rung(EscalationType.Resolution, 1, 120, EscalationRecipient.TeamLead));

        replaced.Value.ResolutionLevelCount.ShouldBe(1);
        var escalations = (await SearchPoliciesAsync()).Value.Rows.Single().Escalations;
        escalations.Single().ThresholdPercent.ShouldBe(120);
    }

    [Fact]
    public async Task Thresholds_have_to_climb()
    {
        // Level 2 firing before level 1 is not a ladder, and the worker walks
        // them in level order.
        await fixture.ResetAsync();
        var id = (await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 480)).Value.Id;

        var result = await SetEscalationsAsync(id,
            Rung(EscalationType.Resolution, 1, 200, EscalationRecipient.AssignedTechnician),
            Rung(EscalationType.Resolution, 2, 150, EscalationRecipient.TeamLead));

        result.Error!.Code.ShouldBe("SlaEscalation.ThresholdOrder");
    }

    [Fact]
    public async Task Response_and_resolution_ladders_are_independent()
    {
        await fixture.ResetAsync();
        var id = (await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 480)).Value.Id;

        var set = await SetEscalationsAsync(id,
            Rung(EscalationType.Response, 1, 100, EscalationRecipient.AssignedTechnician),
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.TeamLead));

        set.Value.ResponseLevelCount.ShouldBe(1);
        set.Value.ResolutionLevelCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_level_cannot_appear_twice()
    {
        await fixture.ResetAsync();
        var id = (await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 480)).Value.Id;

        var result = await SetEscalationsAsync(id,
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician),
            Rung(EscalationType.Resolution, 1, 150, EscalationRecipient.TeamLead));

        result.Error!.Code.ShouldBe("SlaEscalation.DuplicateLevel");
    }

    [Fact]
    public async Task A_level_is_one_through_four()
    {
        await fixture.ResetAsync();
        var id = (await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 480)).Value.Id;

        (await SetEscalationsAsync(id,
            Rung(EscalationType.Resolution, 5, 100, EscalationRecipient.AssignedTechnician)))
            .Error!.Code.ShouldBe("SlaEscalation.Level");
    }

    [Fact]
    public async Task A_custom_recipient_needs_an_address_and_nobody_else_keeps_one()
    {
        await fixture.ResetAsync();
        var id = (await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 480)).Value.Id;

        (await SetEscalationsAsync(id,
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.Custom)))
            .Error!.Code.ShouldBe("SlaEscalation.CustomAddress");

        await SetEscalationsAsync(id,
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.TeamLead,
                address: "stray@fujitec.co.in"));

        (await SearchPoliciesAsync()).Value.Rows.Single().Escalations.Single()
            .RecipientAddress.ShouldBeNull();
    }

    [Fact]
    public async Task An_unknown_type_recipient_or_channel_is_refused()
    {
        await fixture.ResetAsync();
        var id = (await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 480)).Value.Id;

        (await SetEscalationsAsync(id, Rung("Whenever", 1, 100, EscalationRecipient.TeamLead)))
            .Error!.Code.ShouldBe("SlaEscalation.UnknownType");

        (await SetEscalationsAsync(id, Rung(EscalationType.Resolution, 1, 100, "Everybody")))
            .Error!.Code.ShouldBe("SlaEscalation.UnknownRecipient");

        (await SetEscalationsAsync(id,
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.TeamLead, channel: "Pigeon")))
            .Error!.Code.ShouldBe("SlaEscalation.UnknownChannel");
    }

    [Fact]
    public async Task A_ladder_needs_a_policy_that_exists()
    {
        await fixture.ResetAsync();

        (await SetEscalationsAsync(987654,
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.TeamLead)))
            .Error!.Code.ShouldBe("SlaPolicy.NotFound");
    }

    [Fact]
    public async Task The_escalation_log_reads_back_what_fired()
    {
        // "Nobody told me" is answerable only if what was sent, to whom and
        // when is recorded — and a Failed row is as much of an answer.
        await fixture.ResetAsync();
        var policyId = (await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 480)).Value.Id;
        await SetEscalationsAsync(policyId,
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.TeamLead));

        await using (var db = fixture.NewContext())
        {
            var escalation = await db.SlaEscalations.SingleAsync(
                TestContext.Current.CancellationToken);

            db.SlaEscalationLogs.Add(new SlaEscalationLog
            {
                ServiceRequestId = 42,
                SlaEscalationId = escalation.Id,
                EscalationType = EscalationType.Resolution,
                Level = 1,
                SentTo = "lead@fujitec.co.in",
                Channel = EscalationChannel.Email,
                Outcome = EscalationOutcome.Failed,
                FailureReason = "SMTP host refused the connection.",
                FiredOnUtc = fixture.Clock.UtcNow,
            });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var row = (await SearchLogAsync(ticketId: 42)).Value.Rows.Single();
        row.Outcome.ShouldBe(EscalationOutcome.Failed);
        row.FailureReason.ShouldNotBeNull();
    }

    // ------------------------------------------------------ the calculator

    [Fact]
    public async Task A_ticket_with_no_matching_policy_gets_no_targets()
    {
        // An ordinary answer. A site that has not configured SLA still raises
        // tickets; they simply have no due date.
        await fixture.ResetAsync();

        var targets = await ComputeAsync(SlaPriority.High, 1, Ist(2026, 8, 3, 10, 0));

        targets.ShouldBeNull();
    }

    [Fact]
    public async Task Targets_are_measured_in_operational_minutes()
    {
        // Four working hours from 15:00 on a nine-to-six Monday is 10:00 on
        // Tuesday, not 19:00 on Monday.
        await fixture.ResetAsync();
        await SetCalendarAsync(1);
        await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 240);

        var targets = await ComputeAsync(SlaPriority.Medium, 1, Ist(2026, 8, 3, 15, 0));

        targets.ShouldNotBeNull();
        targets.ResolutionDueOnUtc.ShouldBe(Ist(2026, 8, 4, 10, 0));
    }

    [Fact]
    public async Task A_ticket_raised_out_of_hours_starts_when_the_branch_opens()
    {
        await fixture.ResetAsync();
        await SetCalendarAsync(1);
        await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 240);

        var targets = await ComputeAsync(SlaPriority.Medium, 1, Ist(2026, 8, 3, 22, 0));

        targets.ShouldNotBeNull();
        targets.IsScheduledHold.ShouldBeTrue();
        targets.StartOnUtc.ShouldBe(Ist(2026, 8, 4, 9, 0));
        targets.ScheduleHoldReason.ShouldNotBeNull();
    }

    [Fact]
    public async Task A_policy_that_ignores_the_calendar_measures_wall_clock()
    {
        // A production outage does not wait for Monday.
        await fixture.ResetAsync();
        await SetCalendarAsync(1);
        await CreatePolicyAsync(
            "Critical", SlaPriority.Critical, 15, 60,
            respectHours: false, respectHolidays: false, respectWeekends: false);

        var raised = Ist(2026, 8, 8, 22, 0);   // a Saturday night
        var targets = await ComputeAsync(SlaPriority.Critical, 1, raised);

        targets.ShouldNotBeNull();
        targets.IsScheduledHold.ShouldBeFalse();
        targets.StartOnUtc.ShouldBe(raised);
        targets.ResolutionDueOnUtc.ShouldBe(raised.AddHours(1));
    }

    [Fact]
    public async Task A_policy_that_ignores_weekends_still_stops_at_closing_time()
    {
        // The three flags are independent: ignoring weekends does not mean
        // ignoring opening hours.
        await fixture.ResetAsync();
        await SetCalendarAsync(1);
        await CreatePolicyAsync(
            "Weekend cover", SlaPriority.High, 60, 240, respectWeekends: false);

        // Saturday 15:00 plus four working hours: three today, one on Sunday.
        var targets = await ComputeAsync(SlaPriority.High, 1, Ist(2026, 8, 8, 15, 0));

        targets.ShouldNotBeNull();
        targets.ResolutionDueOnUtc.ShouldBe(Ist(2026, 8, 9, 10, 0));
    }

    [Fact]
    public async Task A_policy_that_ignores_holidays_works_through_them()
    {
        await fixture.ResetAsync();
        await SetCalendarAsync(1);
        await CreateHolidayAsync("Local festival", new DateOnly(2026, 8, 4));
        await CreatePolicyAsync(
            "Through holidays", SlaPriority.High, 60, 240, respectHolidays: false);
        await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 240);

        var ignoring = await ComputeAsync(SlaPriority.High, 1, Ist(2026, 8, 3, 15, 0));
        var respecting = await ComputeAsync(SlaPriority.Medium, 1, Ist(2026, 8, 3, 15, 0));

        ignoring!.ResolutionDueOnUtc.ShouldBe(Ist(2026, 8, 4, 10, 0));
        respecting!.ResolutionDueOnUtc.ShouldBe(Ist(2026, 8, 5, 10, 0));
    }

    [Fact]
    public async Task Operational_minutes_over_a_weekend_are_nothing()
    {
        // The answer ServiceDesk charges its clock with.
        await fixture.ResetAsync();
        await SetCalendarAsync(1);
        var policyId = (await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 480)).Value.Id;

        var minutes = await MinutesAsync(
            1, Ist(2026, 8, 8, 9, 0), Ist(2026, 8, 10, 9, 0), policyId);

        minutes.ShouldBe(0);
    }

    [Fact]
    public async Task Operational_minutes_with_no_policy_are_wall_clock()
    {
        // Returning zero would make a ticket with no policy consume nothing and
        // look permanently untouched.
        await fixture.ResetAsync();
        await SetCalendarAsync(1);

        var minutes = await MinutesAsync(
            1, Ist(2026, 8, 8, 9, 0), Ist(2026, 8, 8, 11, 0), slaPolicyId: null);

        minutes.ShouldBe(120);
    }

    [Fact]
    public async Task A_branch_with_no_calendar_gets_the_default_working_week()
    {
        await fixture.ResetAsync();
        var policyId = (await CreatePolicyAsync("Standard", SlaPriority.Medium, 60, 240)).Value.Id;

        var targets = await ComputeAsync(SlaPriority.Medium, 2, Ist(2026, 8, 3, 15, 0));

        targets.ShouldNotBeNull();
        targets.SlaPolicyId.ShouldBe(policyId);
        targets.ResolutionDueOnUtc.ShouldBe(Ist(2026, 8, 4, 10, 0));
    }

    // --------------------------------------------------------------- plumbing

    private static readonly TimeZoneInfo India =
        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    private static DateTime Ist(int year, int month, int day, int hour, int minute) =>
        TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified), India);

    private static SetSlaEscalationsCommand.Rung Rung(
        string type,
        int level,
        int threshold,
        string recipient,
        string? address = null,
        string channel = EscalationChannel.Email) =>
        new(type, level, threshold, recipient, address, channel);

    private SlaCalculator NewCalculator()
    {
        var context = fixture.NewContext();

        return new SlaCalculator(context, new CalendarLoader(context, fixture.Locations));
    }

    private Task<SlaTargets?> ComputeAsync(string priority, int? locationId, DateTime raisedUtc) =>
        NewCalculator().ComputeTargetsAsync(
            new SlaTargetRequest(priority, locationId, raisedUtc),
            TestContext.Current.CancellationToken);

    private Task<int> MinutesAsync(int? locationId, DateTime from, DateTime to, int? slaPolicyId) =>
        NewCalculator().OperationalMinutesAsync(
            locationId, from, to, slaPolicyId, TestContext.Current.CancellationToken);

    private Task<Result<CreateSlaPolicyResponse>> CreatePolicyAsync(
        string name,
        string priority,
        int responseMinutes,
        int resolutionMinutes,
        bool respectHours = true,
        bool respectHolidays = true,
        bool respectWeekends = true)
    {
        var handler = new CreateSlaPolicyHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new CreateSlaPolicyCommand(
                name, null, priority, responseMinutes, resolutionMinutes,
                respectHours, respectHolidays, respectWeekends, 30),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<UpdateSlaPolicyResponse>> UpdatePolicyAsync(
        int id,
        string name,
        int responseMinutes,
        int resolutionMinutes,
        bool isActive = true)
    {
        var handler = new UpdateSlaPolicyHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new UpdateSlaPolicyCommand(
                id, name, null, responseMinutes, resolutionMinutes, true, true, true, 30, isActive),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchSlaPoliciesResponse>> SearchPoliciesAsync()
    {
        var handler = new SearchSlaPoliciesHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchSlaPoliciesQuery(null, false), TestContext.Current.CancellationToken);
    }

    private Task<Result<SetSlaEscalationsResponse>> SetEscalationsAsync(
        int policyId, params SetSlaEscalationsCommand.Rung[] levels)
    {
        var handler = new SetSlaEscalationsHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new SetSlaEscalationsCommand(policyId, levels),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchEscalationLogResponse>> SearchLogAsync(int ticketId)
    {
        var handler = new SearchEscalationLogHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchEscalationLogQuery(ticketId, null, 100),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SetLocationCalendarResponse>> SetCalendarAsync(int locationId)
    {
        var context = fixture.NewContext();

        var handler = new SetLocationCalendarHandler(
            context, fixture.Locations, new CalendarLoader(context, fixture.Locations),
            fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new SetLocationCalendarCommand(
                locationId, false, new TimeOnly(9, 0), new TimeOnly(18, 0), null, null, 0, false,
                [.. Enumerable.Range(0, 7).Select(day => new SetLocationCalendarCommand.Day(
                    (byte)day, day is >= 1 and <= 5, CalendarDayType.Standard,
                    null, null, null, null))],
                []),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<CreateHolidayResponse>> CreateHolidayAsync(string name, DateOnly date)
    {
        var handler = new CreateHolidayHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new CreateHolidayCommand(name, date, HolidayType.Festival, true, false, null, []),
            TestContext.Current.CancellationToken);
    }
}
