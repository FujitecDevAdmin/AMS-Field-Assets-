using AMS.Modules.Notifications.PublicApi.Notifications;
using AMS.Modules.ServiceDesk.PublicApi.ServiceDesk;
using AMS.Modules.ServiceLevel.Calendar;
using AMS.Modules.ServiceLevel.Domain;
using AMS.Modules.ServiceLevel.Escalation;
using AMS.Modules.ServiceLevel.Features.CreateSlaPolicy;
using AMS.Modules.ServiceLevel.Features.SetLocationCalendar;
using AMS.Modules.ServiceLevel.Features.SetSlaEscalations;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Tests;

/// <summary>
/// The monitor that tells people a target has been missed: when a rung fires,
/// who it reaches, and why it only fires once.
/// </summary>
[Collection(nameof(ServiceLevelCollectionDefinition))]
public sealed class SlaEscalationTests(ServiceLevelFixture fixture)
{
    private const int Technician = 11;
    private const int TeamLead = 12;
    private const int BranchAdmin = 13;
    private const int ManagerUser = 14;
    private const int Requester = 500;
    private const int ManagerEmployee = 600;

    // -------------------------------------------------------- when it fires

    [Fact]
    public async Task Nothing_fires_before_the_due_time()
    {
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(2)));

        (await RunAsync()).ShouldBe(0);
        Notifier.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_threshold_of_a_hundred_fires_at_the_due_time()
    {
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician));
        Watch(Ticket(policy, resolutionDue: Now.AddMinutes(-1)));

        (await RunAsync()).ShouldBe(1);
        Notifier.Queued.Single().ToAddress.ShouldBe("tech@fujitec.co.in");
    }

    [Fact]
    public async Task A_threshold_past_a_hundred_waits_that_much_longer()
    {
        // ThresholdPercent is ADDITIVE: 150 means half the target again past
        // the due time. That is what lets one ladder serve policies with
        // different targets.
        await fixture.ResetAsync();
        await SetCalendarAsync(1, roundTheClock: true);
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 150, EscalationRecipient.AssignedTechnician),
            resolutionMinutes: 240);

        // Due an hour ago; 150% of a four-hour target is two hours past that.
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-1)));
        (await RunAsync()).ShouldBe(0);

        fixture.Clock.Advance(TimeSpan.FromHours(2));
        (await RunAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task The_grace_period_is_measured_in_operational_minutes()
    {
        // Measuring the target in working hours and the grace period in wall
        // clock would make a Friday-afternoon breach escalate over the weekend.
        await fixture.ResetAsync();
        await SetCalendarAsync(1);
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 200, EscalationRecipient.AssignedTechnician),
            resolutionMinutes: 240);

        // Due at 17:00 on Friday. Another four working hours is Monday at 12:00.
        Watch(Ticket(policy, resolutionDue: Ist(2026, 8, 7, 17, 0)));

        fixture.Clock.UtcNow = Ist(2026, 8, 9, 23, 0);   // Sunday night
        (await RunAsync()).ShouldBe(0);

        fixture.Clock.UtcNow = Ist(2026, 8, 10, 12, 30); // Monday lunchtime
        (await RunAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task A_policy_that_ignores_the_calendar_counts_the_grace_in_wall_clock()
    {
        await fixture.ResetAsync();
        await SetCalendarAsync(1);
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 200, EscalationRecipient.AssignedTechnician),
            resolutionMinutes: 60,
            respectsCalendar: false);

        Watch(Ticket(policy, resolutionDue: Ist(2026, 8, 8, 22, 0)));   // Saturday night

        fixture.Clock.UtcNow = Ist(2026, 8, 8, 23, 30);
        (await RunAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task A_paused_ticket_is_not_late()
    {
        // It is waiting on somebody who is not us. Escalating would be telling
        // a technician off for a delay the requester is causing.
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-5)) with { IsSlaPaused = true });

        (await RunAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task A_response_escalation_is_dropped_once_somebody_has_replied()
    {
        // A complaint about a thing that did not happen.
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Response, 1, 100, EscalationRecipient.AssignedTechnician));
        Watch(Ticket(policy, responseDue: Now.AddHours(-5)) with
        {
            FirstResponseOnUtc = Now.AddHours(-6),
        });

        (await RunAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task A_response_escalation_fires_when_nobody_has_replied()
    {
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Response, 1, 100, EscalationRecipient.AssignedTechnician));
        Watch(Ticket(policy, responseDue: Now.AddHours(-1)));

        (await RunAsync()).ShouldBe(1);
        Notifier.Queued.Single().Subject.ShouldContain("response overdue");
    }

    [Fact]
    public async Task A_ticket_with_no_due_date_for_that_type_is_left_alone()
    {
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Response, 1, 100, EscalationRecipient.AssignedTechnician));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-5)));

        (await RunAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task A_disabled_rung_never_fires()
    {
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician));
        await fixture.ExecuteAsync(
            "UPDATE [ServiceLevel].[SlaEscalation] SET [IsEnabled] = 0;");
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-5)));

        (await RunAsync()).ShouldBe(0);
    }

    // ----------------------------------------------------------- only once

    [Fact]
    public async Task A_rung_fires_once_however_often_the_monitor_runs()
    {
        // The design script's own words: without this, a ticket that stays
        // overdue for a day sends 1,440 e-mails and everybody filters the
        // address.
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-1)));

        await RunAsync();
        Notifier.Reset();

        fixture.Clock.Advance(TimeSpan.FromHours(6));
        (await RunAsync()).ShouldBe(0);
        Notifier.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task Later_rungs_still_fire_after_an_earlier_one_has()
    {
        await fixture.ResetAsync();
        await SetCalendarAsync(1, roundTheClock: true);
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician),
            Rung(EscalationType.Resolution, 2, 200, EscalationRecipient.TeamLead),
            resolutionMinutes: 60);
        Watch(Ticket(policy, resolutionDue: Now.AddMinutes(-1)));

        (await RunAsync()).ShouldBe(1);

        fixture.Clock.Advance(TimeSpan.FromHours(2));
        (await RunAsync()).ShouldBe(1);

        var log = await LogAsync();
        log.Select(l => l.Level).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task A_failed_attempt_can_be_retried()
    {
        // R2-3: the index excludes Outcome = 'Failed', so a failed queue
        // attempt can be tried again while a Sent or Skipped row blocks a
        // repeat.
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician));
        var ticket = Ticket(policy, resolutionDue: Now.AddHours(-1));
        Watch(ticket);
        await RunAsync();

        await fixture.ExecuteAsync(
            $"UPDATE [ServiceLevel].[SlaEscalationLog] SET [Outcome] = N'{EscalationOutcome.Failed}';");
        Notifier.Reset();

        (await RunAsync()).ShouldBe(1);
        Notifier.Queued.Count.ShouldBe(1);
    }

    // ---------------------------------------------------------- who it tells

    [Fact]
    public async Task It_can_tell_the_team_lead()
    {
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.TeamLead));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-1)) with { AssignedTeamId = 3 });
        fixture.Tickets.WithTeamLeads(3, TeamLead);

        await RunAsync();

        Notifier.Queued.Single().ToAddress.ShouldBe("lead@fujitec.co.in");
    }

    [Fact]
    public async Task It_can_tell_the_branch_admin_for_that_branch_only()
    {
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.BranchAdmin));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-1)) with { LocationId = 2 });

        await RunAsync();

        Notifier.Queued.Single().ToAddress.ShouldBe("branch@fujitec.co.in");
    }

    [Fact]
    public async Task It_can_tell_the_requesters_manager()
    {
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.Manager));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-1)));

        await RunAsync();

        Notifier.Queued.Single().ToAddress.ShouldBe("manager@fujitec.co.in");
    }

    [Fact]
    public async Task It_can_tell_a_fixed_address_with_no_account_behind_it()
    {
        // A distribution list, a vendor, a duty phone that turns messages into
        // pages.
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.Custom,
                address: "duty@fujitec.co.in"));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-1)));

        await RunAsync();

        Notifier.Queued.Single().ToAddress.ShouldBe("duty@fujitec.co.in");
    }

    [Fact]
    public async Task A_rung_that_reaches_nobody_is_recorded_rather_than_retried_for_ever()
    {
        // The rung is configured and did not fire, which is a configuration
        // problem somebody has to see — and the row stops the monitor
        // rediscovering the same empty rung every minute.
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-1)) with { AssignedToUserId = null });

        await RunAsync();

        var row = (await LogAsync()).Single();
        row.Outcome.ShouldBe(EscalationOutcome.Skipped);
        row.FailureReason.ShouldNotBeNull();
        Notifier.Queued.ShouldBeEmpty();
        (await RunAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task An_in_app_only_rung_sends_no_e_mail()
    {
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician,
                channel: EscalationChannel.InApp));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-1)));

        await RunAsync();

        Notifier.Queued.ShouldBeEmpty();
        Notifier.Notified.ShouldContain(n => n.UserId == Technician);
        (await LogAsync()).Single().Outcome.ShouldBe(EscalationOutcome.Sent);
    }

    [Fact]
    public async Task A_both_rung_does_both()
    {
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician,
                channel: EscalationChannel.Both));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-1)));

        await RunAsync();

        Notifier.Queued.Count.ShouldBe(1);
        Notifier.Notified.ShouldContain(n => n.UserId == Technician);
    }

    // ------------------------------------------------------- the evidence

    [Fact]
    public async Task What_fired_is_recorded_with_who_it_went_to()
    {
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-1)));

        await RunAsync();

        var row = (await LogAsync()).Single();
        row.ServiceRequestId.ShouldBe(77);
        row.EscalationType.ShouldBe(EscalationType.Resolution);
        row.Level.ShouldBe(1);
        row.SentTo.ShouldBe("tech@fujitec.co.in");
        row.Outcome.ShouldBe(EscalationOutcome.Queued);
        row.EmailOutboxId.ShouldNotBeNull();
        row.FiredOnUtc.ShouldBe(fixture.Clock.UtcNow);
    }

    [Fact]
    public async Task The_e_mail_says_what_asked_for_it()
    {
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-1)));

        await RunAsync();

        var queued = Notifier.Queued.Single();
        queued.SourceType.ShouldBe(EmailSource.SlaEscalation);
        queued.SourceId.ShouldBe(77);
    }

    [Fact]
    public async Task An_escalation_shows_up_on_the_ticket_itself()
    {
        // So somebody reading the ticket can see it went out without going to
        // another screen for it.
        await fixture.ResetAsync();
        var policy = await PolicyWithLadderAsync(
            Rung(EscalationType.Resolution, 1, 100, EscalationRecipient.AssignedTechnician));
        Watch(Ticket(policy, resolutionDue: Now.AddHours(-1)));

        await RunAsync();

        var note = fixture.Tickets.Notes.Single();
        note.TicketId.ShouldBe(77);
        note.Text.ShouldContain("tech@fujitec.co.in");
    }

    // --------------------------------------------------------------- plumbing

    private static readonly TimeZoneInfo India =
        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    private static DateTime Ist(int year, int month, int day, int hour, int minute) =>
        TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified), India);

    private DateTime Now => fixture.Clock.UtcNow;

    private FakeNotifier Notifier => fixture.Notifier;

    private static SlaWatchTicket Ticket(
        int policyId,
        DateTime? responseDue = null,
        DateTime? resolutionDue = null) =>
        new(
            Id: 77,
            RequestNumber: "TKT-2026-000077",
            Subject: "Cannot print",
            Priority: SlaPriority.Medium,
            StatusName: "In Progress",
            SlaPolicyId: policyId,
            LocationId: 1,
            ResponseDueOnUtc: responseDue,
            ResolutionDueOnUtc: resolutionDue,
            FirstResponseOnUtc: null,
            IsSlaPaused: false,
            AssignedToUserId: Technician,
            AssignedTeamId: null,
            RequestedByEmployeeId: Requester);

    private void Watch(SlaWatchTicket ticket) => fixture.Tickets.With(ticket);

    private static SetSlaEscalationsCommand.Rung Rung(
        string type,
        int level,
        int threshold,
        string recipient,
        string? address = null,
        string channel = EscalationChannel.Email) =>
        new(type, level, threshold, recipient, address, channel);

    private async Task<int> PolicyWithLadderAsync(
        params SetSlaEscalationsCommand.Rung[] rungs) =>
        await PolicyWithLadderAsync(rungs, 480, true);

    private async Task<int> PolicyWithLadderAsync(
        SetSlaEscalationsCommand.Rung rung,
        int resolutionMinutes = 480,
        bool respectsCalendar = true) =>
        await PolicyWithLadderAsync([rung], resolutionMinutes, respectsCalendar);

    private async Task<int> PolicyWithLadderAsync(
        SetSlaEscalationsCommand.Rung first,
        SetSlaEscalationsCommand.Rung second,
        int resolutionMinutes = 480) =>
        await PolicyWithLadderAsync([first, second], resolutionMinutes, true);

    private async Task<int> PolicyWithLadderAsync(
        IReadOnlyList<SetSlaEscalationsCommand.Rung> rungs,
        int resolutionMinutes,
        bool respectsCalendar)
    {
        var create = new CreateSlaPolicyHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        var created = await create.HandleAsync(
            new CreateSlaPolicyCommand(
                "Standard", null, SlaPriority.Medium, 60, resolutionMinutes,
                respectsCalendar, respectsCalendar, respectsCalendar, 30),
            TestContext.Current.CancellationToken);

        var set = new SetSlaEscalationsHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        await set.HandleAsync(
            new SetSlaEscalationsCommand(created.Value.Id, rungs),
            TestContext.Current.CancellationToken);

        return created.Value.Id;
    }

    private Task<int> RunAsync()
    {
        var context = fixture.NewContext();

        var monitor = new SlaEscalationMonitor(
            context,
            fixture.Tickets,
            fixture.Notifier,
            fixture.Users,
            fixture.Employees,
            new CalendarLoader(context, fixture.Locations),
            fixture.Clock);

        return monitor.RunAsync(TestContext.Current.CancellationToken);
    }

    private Task<Result<SetLocationCalendarResponse>> SetCalendarAsync(
        int locationId, bool roundTheClock = false)
    {
        var context = fixture.NewContext();

        var handler = new SetLocationCalendarHandler(
            context, fixture.Locations, new CalendarLoader(context, fixture.Locations),
            fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new SetLocationCalendarCommand(
                locationId, roundTheClock, new TimeOnly(9, 0), new TimeOnly(18, 0),
                null, null, 0, false,
                [.. Enumerable.Range(0, 7).Select(day => new SetLocationCalendarCommand.Day(
                    (byte)day, day is >= 1 and <= 5, CalendarDayType.Standard,
                    null, null, null, null))],
                []),
            TestContext.Current.CancellationToken);
    }

    private async Task<List<SlaEscalationLog>> LogAsync()
    {
        await using var db = fixture.NewContext();

        return await db.SlaEscalationLogs
            .OrderBy(l => l.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
    }
}
