using AMS.Modules.ServiceDesk.Approvals;
using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Features.CreateApprovalWorkflow;
using AMS.Modules.ServiceDesk.Features.PublishApprovalWorkflow;
using AMS.Modules.ServiceDesk.Features.RaiseServiceRequest;
using AMS.Modules.ServiceDesk.Features.SubmitForApproval;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.Modules.ServiceDesk.Features.CancelApproval;
using AMS.Modules.ServiceDesk.Features.DecideApproval;
using AMS.Modules.Notifications.PublicApi.Notifications;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Tests;

/// <summary>
/// Approvals reaching people: who is told, when they are chased, and what the
/// log says afterwards.
/// </summary>
[Collection(nameof(ServiceDeskCollectionDefinition))]
public sealed class ApprovalNotificationTests(ServiceDeskFixture fixture)
{
    private const int ManagerUser = 20;
    private const int HeadUser = 30;

    // ------------------------------------------------------------- asking

    [Fact]
    public async Task Submitting_asks_the_first_level()
    {
        // The gap this closes. Resolving approvers and never asking them is an
        // approval that waits for ever.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseAsync();

        await SubmitAsync(ticket);

        var queued = fixture.Notifier.Queued.Single();
        queued.ToAddress.ShouldBe("kumar@fujitec.co.in");
        queued.SourceType.ShouldBe(EmailSource.Approval);
        queued.Subject.ShouldStartWith("Approval needed:");
        fixture.Notifier.Notified.ShouldContain(n => n.UserId == ManagerUser);
    }

    [Fact]
    public async Task Asking_leaves_a_log_row_pointing_at_the_outbox()
    {
        // "Nobody told me" is answerable only if what was sent, to whom and
        // when is recorded.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseAsync();

        await SubmitAsync(ticket);

        var log = await LogAsync();
        var row = log.Single();
        row.NotificationType.ShouldBe(ApprovalNotificationType.ApprovalRequired);
        row.Status.ShouldBe(ApprovalNotificationStatus.Queued);
        row.RecipientAddress.ShouldBe("kumar@fujitec.co.in");
        row.EmailOutboxId.ShouldNotBeNull();
    }

    [Fact]
    public async Task Somebody_with_no_address_is_recorded_as_skipped()
    {
        // "We did not tell them" is a fact worth having, and an empty log is
        // indistinguishable from a worker that never ran.
        //
        // It is the SUBMITTER who can be in this state, not an approver: the
        // resolver drops anybody with no address at submission, so a level
        // cannot end up waiting on somebody who could not be asked.
        await fixture.ResetAsync();
        fixture.Users.With(41, "No Mailbox", email: null);
        var submitter = new TestCurrentUser { Id = 41, Username = "no-mailbox" };
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket, submitter);
        fixture.Notifier.Reset();

        await DecideAsync(await FirstParticipantAsync(ticket), true, ManagerUser);

        var announcement = (await LogAsync())
            .Single(l => l.NotificationType == ApprovalNotificationType.RequestApproved);
        announcement.Status.ShouldBe(ApprovalNotificationStatus.Skipped);
        announcement.LastError.ShouldNotBeNull();
        // Still told in-app. A user with no mailbox still has a bell.
        fixture.Notifier.Notified.ShouldContain(n => n.UserId == 41);
        fixture.Notifier.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task Approving_a_level_asks_the_next_one()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Two levels",
            Stage("Manager", ApprovalMode.Any, ManagerRule()),
            Stage("IT head", ApprovalMode.Any, UserRule(HeadUser)));
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket);
        fixture.Notifier.Reset();

        await DecideAsync(await FirstParticipantAsync(ticket), true, ManagerUser);

        fixture.Notifier.Queued.Single().ToAddress.ShouldBe("iyer@fujitec.co.in");
    }

    // ------------------------------------------------------- announcing

    [Fact]
    public async Task Approving_the_last_level_tells_whoever_asked()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket);
        fixture.Notifier.Reset();

        await DecideAsync(await FirstParticipantAsync(ticket), true, ManagerUser);

        fixture.Notifier.Notified.ShouldContain(n =>
            n.UserId == fixture.CurrentUser.Id && n.Text.Contains("approved"));
        (await LogAsync()).ShouldContain(l =>
            l.NotificationType == ApprovalNotificationType.RequestApproved);
    }

    [Fact]
    public async Task Rejecting_tells_whoever_asked()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket);
        fixture.Notifier.Reset();

        await DecideAsync(await FirstParticipantAsync(ticket), false, ManagerUser);

        (await LogAsync()).ShouldContain(l =>
            l.NotificationType == ApprovalNotificationType.RequestRejected);
    }

    [Fact]
    public async Task Settling_a_level_tells_the_approvers_who_never_answered()
    {
        // In-app only. An e-mail saying "never mind" is how people learn to
        // filter the one that asked.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Either of us",
            Stage("Panel", ApprovalMode.Any, ManagerRule(), UserRule(HeadUser)));
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket);
        var participants = await ParticipantsAsync(ticket);
        var acting = participants[0];
        fixture.Notifier.Reset();

        await DecideAsync(acting.Id, true, acting.ApproverUserId!.Value);

        var other = participants.Single(p => p.Id != acting.Id);
        fixture.Notifier.Notified.ShouldContain(n =>
            n.UserId == other.ApproverUserId && n.Text.Contains("settled"));
    }

    [Fact]
    public async Task Cancelling_tells_the_submitter_and_empties_the_approvers_lists()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket);
        fixture.Notifier.Reset();

        await CancelAsync(ticket, "The joiner declined the offer.");

        (await LogAsync()).ShouldContain(l =>
            l.NotificationType == ApprovalNotificationType.RequestCancelled);
        fixture.Notifier.Notified.ShouldContain(n =>
            n.UserId == ManagerUser && n.Text.Contains("cancelled"));
    }

    // -------------------------------------------------------- the chasing

    [Fact]
    public async Task Nothing_is_chased_before_its_reminder_is_due()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Chased",
            Stage("Manager", ApprovalMode.Any, ManagerRule()) with
            {
                DueAfterMinutes = 120,
                ReminderAfterMinutes = 60,
            });
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket);
        fixture.Notifier.Reset();

        fixture.Clock.Advance(TimeSpan.FromMinutes(30));

        (await RunWorkerAsync()).ShouldBe(0);
        fixture.Notifier.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_approver_who_has_not_answered_is_reminded()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Chased",
            Stage("Manager", ApprovalMode.Any, ManagerRule()) with
            {
                DueAfterMinutes = 120,
                ReminderAfterMinutes = 60,
            });
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket);
        fixture.Notifier.Reset();

        fixture.Clock.Advance(TimeSpan.FromMinutes(90));
        var sent = await RunWorkerAsync();

        sent.ShouldBe(1);
        fixture.Notifier.Queued.Single().Subject.ShouldStartWith("Still waiting:");
        (await LogAsync()).ShouldContain(l =>
            l.NotificationType == ApprovalNotificationType.Reminder);
    }

    [Fact]
    public async Task The_same_reminder_is_not_sent_twice()
    {
        // UX_ApprovalNotificationLog_Idempotency, and a key derived from what
        // the message IS rather than a random Guid. A worker that restarts
        // mid-pass must not send everybody the same thing again.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Chased",
            Stage("Manager", ApprovalMode.Any, ManagerRule()) with
            {
                DueAfterMinutes = 120,
                ReminderAfterMinutes = 60,
            });
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket);
        fixture.Clock.Advance(TimeSpan.FromMinutes(90));
        await RunWorkerAsync();
        fixture.Notifier.Reset();

        await RunWorkerAsync();

        fixture.Notifier.Queued.ShouldBeEmpty();
        (await LogAsync()).Count(l => l.NotificationType == ApprovalNotificationType.Reminder)
            .ShouldBe(1);
    }

    [Fact]
    public async Task A_repeating_reminder_goes_again_at_the_next_interval()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Nagged",
            Stage("Manager", ApprovalMode.Any, ManagerRule()) with
            {
                DueAfterMinutes = 600,
                ReminderAfterMinutes = 60,
                ReminderRepeatMinutes = 60,
            });
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket);

        fixture.Clock.Advance(TimeSpan.FromMinutes(60));
        await RunWorkerAsync();
        fixture.Clock.Advance(TimeSpan.FromMinutes(60));
        await RunWorkerAsync();

        (await LogAsync()).Count(l => l.NotificationType == ApprovalNotificationType.Reminder)
            .ShouldBe(2);
    }

    [Fact]
    public async Task A_stalled_approval_escalates_to_whoever_asked_for_it()
    {
        // The stage timer says when, not to whom — there is no recipient rule
        // for approvals — so it goes up to the submitter.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Escalated",
            Stage("Manager", ApprovalMode.Any, ManagerRule()) with
            {
                DueAfterMinutes = 60,
                EscalateAfterMinutes = 60,
            });
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket);
        fixture.Notifier.Reset();

        fixture.Clock.Advance(TimeSpan.FromMinutes(150));
        await RunWorkerAsync();

        (await LogAsync()).ShouldContain(l =>
            l.NotificationType == ApprovalNotificationType.Escalation);
        fixture.Notifier.Notified.ShouldContain(n =>
            n.UserId == fixture.CurrentUser.Id && n.Text.Contains("stuck"));
    }

    [Fact]
    public async Task An_escalation_happens_once()
    {
        // Telling somebody every hour that a thing is still stuck is how they
        // stop reading it.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Escalated",
            Stage("Manager", ApprovalMode.Any, ManagerRule()) with
            {
                DueAfterMinutes = 60,
                EscalateAfterMinutes = 60,
            });
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket);

        fixture.Clock.Advance(TimeSpan.FromMinutes(150));
        await RunWorkerAsync();
        fixture.Clock.Advance(TimeSpan.FromMinutes(500));
        await RunWorkerAsync();

        (await LogAsync()).Count(l => l.NotificationType == ApprovalNotificationType.Escalation)
            .ShouldBe(1);
    }

    [Fact]
    public async Task A_decided_approval_is_not_chased()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Chased",
            Stage("Manager", ApprovalMode.Any, ManagerRule()) with
            {
                DueAfterMinutes = 60,
                ReminderAfterMinutes = 30,
            });
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket);
        await DecideAsync(await FirstParticipantAsync(ticket), true, ManagerUser);
        fixture.Notifier.Reset();

        fixture.Clock.Advance(TimeSpan.FromDays(1));

        (await RunWorkerAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task A_stage_with_no_timers_is_never_chased()
    {
        // The columns are nullable, and null means "do not".
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Untimed");
        var ticket = await RaiseAsync();
        await SubmitAsync(ticket);

        fixture.Clock.Advance(TimeSpan.FromDays(30));

        (await RunWorkerAsync()).ShouldBe(0);
    }

    // ------------------------------------------------- the arithmetic alone

    [Theory]
    [InlineData(30, 0)]
    [InlineData(60, 1)]
    [InlineData(119, 1)]
    [InlineData(120, 2)]
    [InlineData(300, 5)]
    public void A_repeating_reminder_counts_from_the_activation_time(int elapsed, int expected)
    {
        // Counted rather than tracked in a column, so a worker that was
        // switched off for a day comes back and sends the reminder due NOW, not
        // the four it missed.
        var stage = new ApprovalWorkflowStage
        {
            StageName = "Manager",
            ApprovalMode = ApprovalMode.Any,
            ReminderAfterMinutes = 60,
            ReminderRepeatMinutes = 60,
        };

        var activated = new DateTime(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);

        ApprovalSchedule
            .ReminderOccurrence(stage, activated, activated.AddMinutes(elapsed))
            .ShouldBe(expected);
    }

    [Fact]
    public void A_reminder_that_does_not_repeat_is_sent_once()
    {
        var stage = new ApprovalWorkflowStage
        {
            StageName = "Manager",
            ApprovalMode = ApprovalMode.Any,
            ReminderAfterMinutes = 60,
        };

        var activated = new DateTime(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);

        ApprovalSchedule
            .ReminderOccurrence(stage, activated, activated.AddMinutes(600))
            .ShouldBe(1);
    }

    [Fact]
    public void A_stage_that_can_never_become_due_never_escalates()
    {
        // EscalateAfterMinutes is measured after the step becomes due, and a
        // stage with no due time never does.
        var stage = new ApprovalWorkflowStage
        {
            StageName = "Manager",
            ApprovalMode = ApprovalMode.Any,
            EscalateAfterMinutes = 60,
        };

        var step = new RequestApprovalStep
        {
            StageNameSnapshot = "Manager",
            ApprovalModeSnapshot = ApprovalMode.Any,
            Status = ApprovalStepStatus.Pending,
            DueOnUtc = null,
        };

        ApprovalSchedule
            .EscalationOccurrence(step, stage, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .ShouldBe(0);
    }

    [Fact]
    public void The_same_message_always_gets_the_same_idempotency_key()
    {
        DeterministicGuid.From(ApprovalNotificationType.Reminder, 1L, 2L, 3L, 1)
            .ShouldBe(DeterministicGuid.From(ApprovalNotificationType.Reminder, 1L, 2L, 3L, 1));

        DeterministicGuid.From(ApprovalNotificationType.Reminder, 1L, 2L, 3L, 1)
            .ShouldNotBe(DeterministicGuid.From(ApprovalNotificationType.Reminder, 1L, 2L, 3L, 2));
    }

    // --------------------------------------------------------------- plumbing

    private static CreateApprovalWorkflowCommand.Rule ManagerRule() =>
        new(ResolverType.RequesterManager, null, null, null, null, null, true);

    private static CreateApprovalWorkflowCommand.Rule UserRule(int userId) =>
        new(ResolverType.User, userId, null, null, null, null, true);

    private static CreateApprovalWorkflowCommand.Stage Stage(
        string name,
        string mode,
        params CreateApprovalWorkflowCommand.Rule[] rules) =>
        new(0, name, mode, null, null, null, null, false, rules);

    private ApprovalNotifications NewNotifications(ServiceDeskDbContext context) =>
        new(context, fixture.Notifier, fixture.Users, fixture.Clock);

    private async Task<int> PublishedWorkflowAsync(
        string name,
        params CreateApprovalWorkflowCommand.Stage[] stages)
    {
        var create = new CreateApprovalWorkflowHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        var numbered = stages.Length == 0
            ? [Stage("Manager", ApprovalMode.Any, ManagerRule()) with { StageNumber = 1 }]
            : stages.Select((s, i) => s with { StageNumber = i + 1 }).ToList();

        var created = await create.HandleAsync(
            new CreateApprovalWorkflowCommand(name, null, null, null, null, false, numbered),
            TestContext.Current.CancellationToken);

        var publish = new PublishApprovalWorkflowHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        await publish.HandleAsync(
            new PublishApprovalWorkflowCommand(created.Value.Id, true, true, null, null),
            TestContext.Current.CancellationToken);

        return created.Value.Id;
    }

    private async Task<int> RaiseAsync()
    {
        var handler = new RaiseServiceRequestHandler(
            fixture.NewContext(), fixture.Sla, fixture.Clock, fixture.CurrentUser,
            fixture.SqlErrors);

        var raised = await handler.HandleAsync(
            new RaiseServiceRequestCommand(
                RequestKind.NewService, "New joiner", null, RequestPriority.Medium,
                null, null, null, null, null, 100, null, null,
                new RaiseServiceRequestCommand.NewServiceDetail(
                    true, true, false, false, null, null, [])),
            TestContext.Current.CancellationToken);

        return raised.Value.Id;
    }

    private async Task SubmitAsync(int ticketId, TestCurrentUser? submitter = null)
    {
        var context = fixture.NewContext();

        var handler = new SubmitForApprovalHandler(
            context,
            new ApproverResolver(fixture.Users, fixture.Employees),
            NewNotifications(context),
            fixture.Clock,
            submitter ?? fixture.CurrentUser,
            fixture.SqlErrors);

        await handler.HandleAsync(
            new SubmitForApprovalCommand(ticketId, null), TestContext.Current.CancellationToken);
    }

    private async Task DecideAsync(long participantId, bool approved, int asUser)
    {
        var caller = new TestCurrentUser { Id = asUser, Username = $"user-{asUser}" };
        var context = fixture.NewContext();

        var handler = new DecideApprovalHandler(
            context,
            new ApproverResolver(fixture.Users, fixture.Employees),
            NewNotifications(context),
            fixture.Clock,
            caller,
            fixture.SqlErrors);

        await handler.HandleAsync(
            new DecideApprovalCommand(
                participantId, Guid.NewGuid(), approved, null, DecisionSource.Application),
            TestContext.Current.CancellationToken);
    }

    private async Task CancelAsync(int ticketId, string reason)
    {
        var context = fixture.NewContext();

        var handler = new CancelApprovalHandler(
            context, NewNotifications(context), fixture.Clock, fixture.CurrentUser);

        await handler.HandleAsync(
            new CancelApprovalCommand(ticketId, reason), TestContext.Current.CancellationToken);
    }

    private Task<int> RunWorkerAsync()
    {
        var context = fixture.NewContext();

        var worker = new ApprovalReminderWorker(
            context, NewNotifications(context), fixture.Clock);

        return worker.RunAsync(TestContext.Current.CancellationToken);
    }

    private async Task<List<RequestApprovalParticipant>> ParticipantsAsync(int ticketId)
    {
        await using var db = fixture.NewContext();

        return await (
            from p in db.RequestApprovalParticipants
            join s in db.RequestApprovalSteps on p.RequestApprovalStepId equals s.Id
            join i in db.RequestApprovalInstances on s.RequestApprovalInstanceId equals i.Id
            where i.ServiceRequestId == ticketId && s.Status == ApprovalStepStatus.Pending
            orderby p.Id
            select p).ToListAsync(TestContext.Current.CancellationToken);
    }

    private async Task<long> FirstParticipantAsync(int ticketId) =>
        (await ParticipantsAsync(ticketId))[0].Id;

    private async Task<List<ApprovalNotificationLog>> LogAsync()
    {
        await using var db = fixture.NewContext();

        return await db.ApprovalNotificationLogs
            .OrderBy(l => l.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
    }
}
