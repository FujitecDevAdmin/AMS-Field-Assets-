using AMS.Modules.ServiceDesk.Approvals;
using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Features.CancelApproval;
using AMS.Modules.ServiceDesk.Features.CreateApprovalWorkflow;
using AMS.Modules.ServiceDesk.Features.DecideApproval;
using AMS.Modules.ServiceDesk.Features.GetRequestApproval;
using AMS.Modules.ServiceDesk.Features.PublishApprovalWorkflow;
using AMS.Modules.ServiceDesk.Features.RaiseServiceRequest;
using AMS.Modules.ServiceDesk.Features.SearchApprovalWorkflows;
using AMS.Modules.ServiceDesk.Features.SearchMyApprovals;
using AMS.Modules.ServiceDesk.Features.SubmitForApproval;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Tests;

/// <summary>
/// The approval workflow: routes and their versions, and the runs they produce.
/// Pass three of three.
/// </summary>
[Collection(nameof(ServiceDeskCollectionDefinition))]
public sealed class ApprovalTests(ServiceDeskFixture fixture)
{
    private const int Requester = 100;
    private const int Manager = 200;
    private const int ManagerUser = 20;
    private const int HeadUser = 30;

    // ----------------------------------------------------------- the route

    [Fact]
    public async Task A_route_is_created_as_a_draft()
    {
        // Publishing is a separate, deliberate act. A route that went live the
        // moment somebody saved it could approve something half-configured.
        await fixture.ResetAsync();

        var created = await CreateWorkflowAsync("Joiner approval");

        created.IsSuccess.ShouldBeTrue();
        created.Value.VersionNumber.ShouldBe(1);
        var row = (await SearchWorkflowsAsync()).Value.Rows.Single();
        row.IsPublished.ShouldBeFalse();
        row.Stages.Single().Rules.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Sending_the_same_name_again_makes_the_next_version()
    {
        // A published definition is never edited: editing one in place would
        // rewrite the rules an in-flight approval is being judged by.
        await fixture.ResetAsync();
        await CreateWorkflowAsync("Joiner approval");

        var second = await CreateWorkflowAsync("Joiner approval");

        second.Value.VersionNumber.ShouldBe(2);
        (await SearchWorkflowsAsync()).Value.Rows.Select(r => r.VersionNumber).ShouldBe([2, 1]);
    }

    [Fact]
    public async Task Stages_are_numbered_in_the_order_they_arrive()
    {
        await fixture.ResetAsync();

        var created = await CreateWorkflowAsync(
            "Two levels",
            Stage("Manager", ApprovalMode.Any, ManagerRule()),
            Stage("IT head", ApprovalMode.All, UserRule(HeadUser)));

        created.Value.StageCount.ShouldBe(2);
        var stages = (await SearchWorkflowsAsync()).Value.Rows.Single().Stages;
        stages.Select(s => s.StageNumber).ShouldBe([1, 2]);
        stages[1].StageName.ShouldBe("IT head");
    }

    [Fact]
    public async Task A_level_with_no_approver_rules_is_refused()
    {
        // It would resolve to nobody, and a level waiting on nobody never
        // completes.
        await fixture.ResetAsync();

        var result = await CreateWorkflowAsync("Empty", Stage("Nobody", ApprovalMode.Any));

        result.Error!.Code.ShouldBe("ApprovalWorkflow.StageHasNoApprovers");
    }

    [Fact]
    public async Task An_incomplete_resolver_is_named_rather_than_left_to_the_check_constraint()
    {
        // CK_ApprovalStageApproverRule_Value has seven branches. The 500 it
        // produces tells an administrator nothing about which field is empty.
        await fixture.ResetAsync();

        var result = await CreateWorkflowAsync(
            "Broken",
            Stage("Level", ApprovalMode.Any,
                new CreateApprovalWorkflowCommand.Rule(
                    ResolverType.Role, null, null, null, null, null, true)));

        result.Error!.Code.ShouldBe("ApprovalWorkflow.ResolverIncomplete");
        result.Error.Message.ShouldContain("a role");
    }

    [Fact]
    public async Task An_unknown_resolver_or_mode_is_refused()
    {
        await fixture.ResetAsync();

        (await CreateWorkflowAsync("Odd", Stage("Level", "Most", ManagerRule()))).Error!.Code
            .ShouldBe("ApprovalWorkflow.UnknownMode");

        var badResolver = await CreateWorkflowAsync(
            "Odd",
            Stage("Level", ApprovalMode.Any,
                new CreateApprovalWorkflowCommand.Rule(
                    "Astrology", null, null, null, null, null, true)));
        badResolver.Error!.Code.ShouldBe("ApprovalWorkflow.UnknownResolver");
    }

    [Fact]
    public async Task A_route_with_no_levels_cannot_be_published()
    {
        await fixture.ResetAsync();
        var id = (await CreateWorkflowAsync("Joiner approval")).Value.Id;
        await fixture.ExecuteAsync(
            $"DELETE FROM [ServiceDesk].[ApprovalStageApproverRule];"
            + $"DELETE FROM [ServiceDesk].[ApprovalWorkflowStage] WHERE [ApprovalWorkflowId] = {id};");

        (await PublishAsync(id)).Error!.Code.ShouldBe("ApprovalWorkflow.NoStages");
    }

    [Fact]
    public async Task A_version_with_approvals_still_running_cannot_be_retired()
    {
        await fixture.ResetAsync();
        var id = await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);

        (await PublishAsync(id, isActive: false)).Error!.Code.ShouldBe("ApprovalWorkflow.InFlight");
    }

    [Fact]
    public async Task Retiring_a_route_gives_up_the_default_slot()
    {
        // UX_ApprovalWorkflowDefinition_OneActiveDefault allows one live
        // default. A retired route holding it would block its replacement.
        await fixture.ResetAsync();
        var id = await PublishedWorkflowAsync("Joiner approval");
        await fixture.ExecuteAsync(
            $"UPDATE [ServiceDesk].[ApprovalWorkflowDefinition] SET [IsDefault] = 1 WHERE [Id] = {id};");

        await PublishAsync(id, isActive: false);

        (await SearchWorkflowsAsync()).Value.Rows.Single().IsDefault.ShouldBeFalse();
    }

    [Fact]
    public async Task An_effective_range_must_run_forwards()
    {
        await fixture.ResetAsync();
        var id = (await CreateWorkflowAsync("Joiner approval")).Value.Id;
        var now = fixture.Clock.UtcNow;

        var result = await PublishAsync(id, from: now.AddDays(5), to: now);

        result.Error!.Code.ShouldBe("ApprovalWorkflow.EffectiveRange");
    }

    // -------------------------------------------------------------- submit

    [Fact]
    public async Task Submitting_starts_a_run_and_activates_the_first_level()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();

        var submitted = await SubmitAsync(ticket);

        submitted.IsSuccess.ShouldBeTrue();
        submitted.Value.Status.ShouldBe(ApprovalInstanceStatus.Pending);
        submitted.Value.CurrentStageNumber.ShouldBe(1);
        submitted.Value.ApproverCount.ShouldBe(1);
    }

    [Fact]
    public async Task Only_a_new_service_request_goes_through_approval()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseSupportTicketAsync();

        (await SubmitAsync(ticket)).Error!.Code.ShouldBe("ServiceRequest.NotApprovable");
    }

    [Fact]
    public async Task A_request_cannot_be_submitted_twice()
    {
        // UX_RequestApprovalInstance_OnePending would catch it; saying so here
        // means a double-click gets a sentence rather than an index name.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);

        (await SubmitAsync(ticket)).Error!.Code.ShouldBe("RequestApproval.AlreadyRunning");
    }

    [Fact]
    public async Task Submitting_with_no_published_route_is_refused()
    {
        await fixture.ResetAsync();
        await CreateWorkflowAsync("Still a draft");
        var ticket = await RaiseNewServiceAsync();

        (await SubmitAsync(ticket)).Error!.Code.ShouldBe("ApprovalWorkflow.NoneMatches");
    }

    [Fact]
    public async Task A_route_that_resolves_to_nobody_does_not_start_a_run()
    {
        // It would sit Pending for ever and nothing would chase it.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Nobody home",
            Stage("Manager", ApprovalMode.Any, ManagerRule()));
        var ticket = await RaiseNewServiceAsync(requestedBy: 999);   // no manager on file

        var result = await SubmitAsync(ticket);

        result.Error!.Code.ShouldBe("RequestApproval.NoApprovers");
        (await GetApprovalAsync(ticket)).Error!.Code.ShouldBe("RequestApproval.NotFound");
    }

    [Fact]
    public async Task The_most_specific_matching_route_wins()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Everything");
        await PublishedWorkflowAsync("Critical only", priority: RequestPriority.Critical);
        var ticket = await RaiseNewServiceAsync(priority: RequestPriority.Critical);

        (await SubmitAsync(ticket)).Value.WorkflowName.ShouldBe("Critical only");
    }

    [Fact]
    public async Task The_run_copies_the_route_name_and_version()
    {
        // A year from now the audit should read without the definition still
        // existing under the same name.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();

        var submitted = await SubmitAsync(ticket);

        submitted.Value.WorkflowName.ShouldBe("Joiner approval");
        submitted.Value.WorkflowVersion.ShouldBe(1);
    }

    [Fact]
    public async Task Every_level_is_written_down_at_submission_even_the_ones_not_reached()
    {
        // So the panel can show what is coming.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Two levels",
            Stage("Manager", ApprovalMode.Any, ManagerRule()),
            Stage("IT head", ApprovalMode.Any, UserRule(HeadUser)));
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);

        var steps = (await GetApprovalAsync(ticket)).Value.Steps;
        steps.Count.ShouldBe(2);
        steps[0].Status.ShouldBe(ApprovalStepStatus.Pending);
        steps[1].Status.ShouldBe(ApprovalStepStatus.Waiting);
        steps[1].Participants.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_approver_is_snapshotted_as_they_were_at_submission()
    {
        // A leaver, a rename, a new address: none of them may rewrite who was
        // asked to approve something last month.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);

        var participant = (await GetApprovalAsync(ticket)).Value.Steps[0].Participants.Single();
        participant.ApproverName.ShouldBe("R Kumar");
        participant.ApproverEmail.ShouldBe("kumar@fujitec.co.in");
    }

    [Fact]
    public async Task One_person_reached_by_two_rules_is_one_approver()
    {
        // Asking somebody twice and then waiting for both answers is a level
        // that can never complete.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Doubled up",
            Stage("Level", ApprovalMode.All, ManagerRule(), UserRule(ManagerUser)));
        var ticket = await RaiseNewServiceAsync();

        var submitted = await SubmitAsync(ticket);

        submitted.Value.ApproverCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_branch_admin_rule_only_finds_people_who_can_act_at_that_branch()
    {
        await fixture.ResetAsync();
        fixture.Users.With(41, "Chennai admin", "chennai@fujitec.co.in", capability: "asset.manage", branchId: 1);
        fixture.Users.With(42, "Coimbatore admin", "cbe@fujitec.co.in", capability: "asset.manage", branchId: 2);
        await PublishedWorkflowAsync(
            "Branch admin",
            Stage("Local admin", ApprovalMode.Any,
                new CreateApprovalWorkflowCommand.Rule(
                    ResolverType.LocationBranchAdmin, null, null, "asset.manage", null, null, true)));
        var ticket = await RaiseNewServiceAsync(locationId: 2);

        await SubmitAsync(ticket);

        var participants = (await GetApprovalAsync(ticket)).Value.Steps[0].Participants;
        participants.Single().ApproverName.ShouldBe("Coimbatore admin");
    }

    [Fact]
    public async Task An_external_approver_needs_no_account_at_all()
    {
        // CK_RequestApprovalParticipant_Identity accepts a row with neither a
        // user nor an employee, as long as the address is not empty.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "External",
            Stage("Landlord", ApprovalMode.Any,
                new CreateApprovalWorkflowCommand.Rule(
                    ResolverType.CustomEmail, null, null, null,
                    "landlord@example.com", "The landlord", true)));
        var ticket = await RaiseNewServiceAsync();

        await SubmitAsync(ticket);

        var participant = (await GetApprovalAsync(ticket)).Value.Steps[0].Participants.Single();
        participant.ApproverUserId.ShouldBeNull();
        participant.ApproverEmail.ShouldBe("landlord@example.com");
    }

    // ------------------------------------------------------------ deciding

    [Fact]
    public async Task One_approval_finishes_an_Any_level_and_the_run()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);
        var participant = await FirstParticipantAsync(ticket);

        var decided = await DecideAsync(participant, approved: true, asUser: ManagerUser);

        decided.Value.StepStatus.ShouldBe(ApprovalStepStatus.Approved);
        decided.Value.InstanceStatus.ShouldBe(ApprovalInstanceStatus.Approved);
        decided.Value.CurrentStageNumber.ShouldBeNull();
    }

    [Fact]
    public async Task An_approval_moves_the_run_to_the_next_level()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Two levels",
            Stage("Manager", ApprovalMode.Any, ManagerRule()),
            Stage("IT head", ApprovalMode.Any, UserRule(HeadUser)));
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);

        var decided = await DecideAsync(
            await FirstParticipantAsync(ticket), approved: true, asUser: ManagerUser);

        decided.Value.InstanceStatus.ShouldBe(ApprovalInstanceStatus.Pending);
        decided.Value.CurrentStageNumber.ShouldBe(2);
        var steps = (await GetApprovalAsync(ticket)).Value.Steps;
        steps[1].Status.ShouldBe(ApprovalStepStatus.Pending);
        steps[1].Participants.Single().ApproverUserId.ShouldBe(HeadUser);
    }

    [Fact]
    public async Task One_rejection_sinks_the_level_and_the_run()
    {
        // Under both modes. A level exists to let somebody say no.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Two levels",
            Stage("Manager", ApprovalMode.Any, ManagerRule()),
            Stage("IT head", ApprovalMode.Any, UserRule(HeadUser)));
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);

        var decided = await DecideAsync(
            await FirstParticipantAsync(ticket), approved: false, asUser: ManagerUser);

        decided.Value.InstanceStatus.ShouldBe(ApprovalInstanceStatus.Rejected);
        var steps = (await GetApprovalAsync(ticket)).Value.Steps;
        steps[1].Status.ShouldBe(ApprovalStepStatus.Cancelled);
    }

    [Fact]
    public async Task An_All_level_waits_for_every_required_approver()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Both must agree",
            Stage("Panel", ApprovalMode.All, ManagerRule(), UserRule(HeadUser)));
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);
        var participants = (await GetApprovalAsync(ticket)).Value.Steps[0].Participants;

        var first = await DecideAsync(
            participants[0].Id, approved: true, asUser: participants[0].ApproverUserId!.Value);
        first.Value.InstanceStatus.ShouldBe(ApprovalInstanceStatus.Pending);

        var second = await DecideAsync(
            participants[1].Id, approved: true, asUser: participants[1].ApproverUserId!.Value);
        second.Value.InstanceStatus.ShouldBe(ApprovalInstanceStatus.Approved);
    }

    [Fact]
    public async Task An_optional_approver_cannot_hold_up_an_All_level()
    {
        // Otherwise IsRequired = false would be a decoration.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "One required, one not",
            Stage("Panel", ApprovalMode.All,
                ManagerRule(),
                UserRule(HeadUser) with { IsRequired = false }));
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);
        var participants = (await GetApprovalAsync(ticket)).Value.Steps[0].Participants;
        var required = participants.Single(p => p.IsRequired);

        var decided = await DecideAsync(
            required.Id, approved: true, asUser: required.ApproverUserId!.Value);

        decided.Value.InstanceStatus.ShouldBe(ApprovalInstanceStatus.Approved);
    }

    [Fact]
    public async Task Settling_a_level_closes_out_everybody_who_never_answered()
    {
        // Left Pending they would sit in somebody's My Approvals for ever,
        // asking for a decision that can no longer change anything.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Either of us",
            Stage("Panel", ApprovalMode.Any, ManagerRule(), UserRule(HeadUser)));
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);
        var participants = (await GetApprovalAsync(ticket)).Value.Steps[0].Participants;
        var acting = participants[0];

        await DecideAsync(acting.Id, approved: true, asUser: acting.ApproverUserId!.Value);

        var after = (await GetApprovalAsync(ticket)).Value.Steps[0].Participants;
        after.Single(p => p.Id != acting.Id).ParticipantStatus
            .ShouldBe(ParticipantStatus.Cancelled);
    }

    [Fact]
    public async Task A_retried_decision_returns_the_first_answer_and_does_not_record_a_second()
    {
        // An approval clicked in an e-mail on a bad connection must not become
        // two decisions. UX_RequestApprovalDecision_ClientId makes it so.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);
        var participant = await FirstParticipantAsync(ticket);
        var clientId = Guid.NewGuid();

        var first = await DecideAsync(participant, true, ManagerUser, clientId);
        var replay = await DecideAsync(participant, true, ManagerUser, clientId);

        first.Value.WasAlreadyDecided.ShouldBeFalse();
        replay.Value.WasAlreadyDecided.ShouldBeTrue();
        replay.Value.InstanceStatus.ShouldBe(first.Value.InstanceStatus);

        await using var db = fixture.NewContext();
        (await db.RequestApprovalDecisions.CountAsync(TestContext.Current.CancellationToken))
            .ShouldBe(1);
    }

    [Fact]
    public async Task Somebody_else_cannot_decide_my_approval()
    {
        // An approval recorded against somebody who did not make it is worse
        // than no approval at all.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);

        var result = await DecideAsync(
            await FirstParticipantAsync(ticket), approved: true, asUser: HeadUser);

        result.Error!.Code.ShouldBe("RequestApprovalParticipant.NotYours");
    }

    [Fact]
    public async Task The_same_person_cannot_decide_twice()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Both must agree",
            Stage("Panel", ApprovalMode.All, ManagerRule(), UserRule(HeadUser)));
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);
        var participant = await FirstParticipantAsync(ticket);
        await DecideAsync(participant, true, ManagerUser);

        var again = await DecideAsync(participant, false, ManagerUser, Guid.NewGuid());

        again.Error!.Code.ShouldBe("RequestApprovalParticipant.AlreadyDecided");
    }

    [Fact]
    public async Task A_finished_run_takes_no_more_decisions()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Either of us",
            Stage("Panel", ApprovalMode.Any, ManagerRule(), UserRule(HeadUser)));
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);
        var participants = (await GetApprovalAsync(ticket)).Value.Steps[0].Participants;
        await DecideAsync(participants[0].Id, true, participants[0].ApproverUserId!.Value);

        var late = await DecideAsync(
            participants[1].Id, true, participants[1].ApproverUserId!.Value, Guid.NewGuid());

        late.Error!.Code.ShouldBe("RequestApproval.Finished");
    }

    [Fact]
    public async Task A_decision_records_how_it_reached_us()
    {
        // An approval clicked in a mail client and one made in the application
        // are not equally strong evidence.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);

        await DecideAsync(
            await FirstParticipantAsync(ticket), true, ManagerUser,
            source: DecisionSource.EmailLink);

        var decision = (await GetApprovalAsync(ticket)).Value.Steps[0].Participants.Single().Decision;
        decision.ShouldNotBeNull();
        decision.Source.ShouldBe(DecisionSource.EmailLink);
        decision.ActedByEmail.ShouldBe("kumar@fujitec.co.in");
    }

    [Fact]
    public async Task A_source_the_database_does_not_allow_is_refused()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);

        var result = await DecideAsync(
            await FirstParticipantAsync(ticket), true, ManagerUser, source: "Telepathy");

        result.Error!.Code.ShouldBe("RequestApprovalDecision.UnknownSource");
    }

    [Fact]
    public async Task A_participant_that_does_not_exist_is_a_not_found()
    {
        await fixture.ResetAsync();

        (await DecideAsync(987654, true, ManagerUser)).Error!.Code
            .ShouldBe("RequestApprovalParticipant.NotFound");
    }

    // ------------------------------------------------------------ my inbox

    [Fact]
    public async Task My_approvals_are_the_levels_whose_turn_has_come()
    {
        // A participant on level three of a route still sitting on level one is
        // not being asked for anything yet.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Two levels",
            Stage("Manager", ApprovalMode.Any, ManagerRule()),
            Stage("IT head", ApprovalMode.Any, UserRule(HeadUser)));
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);

        (await MyApprovalsAsync(ManagerUser)).Value.Rows.Count.ShouldBe(1);
        (await MyApprovalsAsync(HeadUser)).Value.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task My_approvals_count_the_overdue_ones()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync(
            "Due in an hour",
            Stage("Manager", ApprovalMode.Any, ManagerRule()) with { DueAfterMinutes = 60 });
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);

        fixture.Clock.Advance(TimeSpan.FromHours(4));

        var inbox = (await MyApprovalsAsync(ManagerUser)).Value;
        inbox.OverdueCount.ShouldBe(1);
        inbox.Rows.Single().IsOverdue.ShouldBeTrue();
    }

    [Fact]
    public async Task A_decided_approval_leaves_my_inbox()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);
        await DecideAsync(await FirstParticipantAsync(ticket), true, ManagerUser);

        (await MyApprovalsAsync(ManagerUser)).Value.Rows.ShouldBeEmpty();
        (await MyApprovalsAsync(ManagerUser, pendingOnly: false)).Value.Rows.Count.ShouldBe(1);
    }

    // ---------------------------------------------------------- cancelling

    [Fact]
    public async Task Cancelling_stops_the_run_and_says_why()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);

        var cancelled = await CancelAsync(ticket, "The joiner declined the offer.");

        cancelled.Value.Status.ShouldBe(ApprovalInstanceStatus.Cancelled);
        var run = (await GetApprovalAsync(ticket)).Value;
        run.CancellationReason.ShouldBe("The joiner declined the offer.");
        run.Steps[0].Status.ShouldBe(ApprovalStepStatus.Cancelled);
    }

    [Fact]
    public async Task Cancelling_empties_the_approvers_inboxes()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);

        await CancelAsync(ticket, "No longer needed.");

        (await MyApprovalsAsync(ManagerUser)).Value.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_cancelled_run_is_kept_not_deleted()
    {
        // R2-12: an approval run is evidence. A run that vanished would leave
        // the request looking as though it had never been submitted.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);
        await CancelAsync(ticket, "Changed our minds.");

        await using var db = fixture.NewContext();
        (await db.RequestApprovalInstances.CountAsync(TestContext.Current.CancellationToken))
            .ShouldBe(1);
        (await db.RequestApprovalParticipants.CountAsync(TestContext.Current.CancellationToken))
            .ShouldBe(1);
    }

    [Fact]
    public async Task A_cancelled_request_can_be_submitted_again()
    {
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);
        await CancelAsync(ticket, "Wrong route.");

        (await SubmitAsync(ticket)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task There_is_nothing_to_cancel_when_nothing_is_running()
    {
        await fixture.ResetAsync();
        var ticket = await RaiseNewServiceAsync();

        (await CancelAsync(ticket, "Nothing here.")).Error!.Code
            .ShouldBe("RequestApproval.NotFound");
    }

    [Fact]
    public async Task The_ticket_timeline_records_what_the_approval_did()
    {
        // Point 7 of the extension's own notes: every material event also lands
        // in RequestHistory, so the request keeps one chronological timeline.
        await fixture.ResetAsync();
        await PublishedWorkflowAsync("Joiner approval");
        var ticket = await RaiseNewServiceAsync();
        await SubmitAsync(ticket);
        await DecideAsync(await FirstParticipantAsync(ticket), true, ManagerUser);

        await using var db = fixture.NewContext();
        var entries = await db.RequestHistories
            .Where(h => h.ServiceRequestId == ticket && h.EntryKind == HistoryEntryKind.Automation)
            .OrderBy(h => h.Id)
            .Select(h => h.EntryText)
            .ToListAsync(TestContext.Current.CancellationToken);

        entries[0].ShouldContain("Submitted for approval");
        entries[^1].ShouldContain("approved");
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

    private Task<Result<CreateApprovalWorkflowResponse>> CreateWorkflowAsync(
        string name,
        params CreateApprovalWorkflowCommand.Stage[] stages)
    {
        var handler = new CreateApprovalWorkflowHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        // The mapper numbers the stages; these helpers go straight to the
        // command, so they do the same.
        var numbered = stages.Length == 0
            ? [Stage("Manager", ApprovalMode.Any, ManagerRule()) with { StageNumber = 1 }]
            : stages.Select((s, i) => s with { StageNumber = i + 1 }).ToList();

        return handler.HandleAsync(
            new CreateApprovalWorkflowCommand(
                name, null, null, null, null, false, numbered),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<PublishApprovalWorkflowResponse>> PublishAsync(
        int id,
        bool isPublished = true,
        bool isActive = true,
        DateTime? from = null,
        DateTime? to = null)
    {
        var handler = new PublishApprovalWorkflowHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new PublishApprovalWorkflowCommand(id, isPublished, isActive, from, to),
            TestContext.Current.CancellationToken);
    }

    private async Task<int> PublishedWorkflowAsync(
        string name,
        params CreateApprovalWorkflowCommand.Stage[] stages)
    {
        var created = await CreateWorkflowAsync(name, stages);
        await PublishAsync(created.Value.Id);

        return created.Value.Id;
    }

    private async Task<int> PublishedWorkflowAsync(string name, string priority)
    {
        var created = await CreateWorkflowAsync(name);
        await fixture.ExecuteAsync(
            $"UPDATE [ServiceDesk].[ApprovalWorkflowDefinition] SET [Priority] = N'{priority}' "
            + $"WHERE [Id] = {created.Value.Id};");
        await PublishAsync(created.Value.Id);

        return created.Value.Id;
    }

    private Task<Result<SearchApprovalWorkflowsResponse>> SearchWorkflowsAsync()
    {
        var handler = new SearchApprovalWorkflowsHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchApprovalWorkflowsQuery(null, false, false, null),
            TestContext.Current.CancellationToken);
    }

    private async Task<int> RaiseNewServiceAsync(
        int requestedBy = Requester,
        int? locationId = null,
        string priority = RequestPriority.Medium)
    {
        var handler = new RaiseServiceRequestHandler(
            fixture.NewContext(), fixture.Sla, fixture.Clock, fixture.CurrentUser,
            fixture.SqlErrors);

        var raised = await handler.HandleAsync(
            new RaiseServiceRequestCommand(
                RequestKind.NewService, "New joiner", null, priority, null, null, null, null, null,
                requestedBy, null, locationId,
                new RaiseServiceRequestCommand.NewServiceDetail(
                    true, true, false, false, null, null, [])),
            TestContext.Current.CancellationToken);

        return raised.Value.Id;
    }

    private async Task<int> RaiseSupportTicketAsync()
    {
        var handler = new RaiseServiceRequestHandler(
            fixture.NewContext(), fixture.Sla, fixture.Clock, fixture.CurrentUser,
            fixture.SqlErrors);

        var raised = await handler.HandleAsync(
            new RaiseServiceRequestCommand(
                RequestKind.SupportTicket, "Cannot print", null, RequestPriority.Medium,
                null, null, null, null, null, Requester, null, null, null),
            TestContext.Current.CancellationToken);

        return raised.Value.Id;
    }

    private Task<Result<SubmitForApprovalResponse>> SubmitAsync(int ticketId)
    {
        var context = fixture.NewContext();

        var handler = new SubmitForApprovalHandler(
            context, NewResolver(), NewNotifications(context), fixture.Clock,
            fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new SubmitForApprovalCommand(ticketId, null), TestContext.Current.CancellationToken);
    }

    private Task<Result<GetRequestApprovalResponse>> GetApprovalAsync(int ticketId)
    {
        var handler = new GetRequestApprovalHandler(fixture.NewContext());

        return handler.HandleAsync(
            new GetRequestApprovalQuery(ticketId), TestContext.Current.CancellationToken);
    }

    private async Task<long> FirstParticipantAsync(int ticketId) =>
        (await GetApprovalAsync(ticketId)).Value.Steps[0].Participants[0].Id;

    private Task<Result<DecideApprovalResponse>> DecideAsync(
        long participantId,
        bool approved,
        int asUser,
        Guid? clientDecisionId = null,
        string source = DecisionSource.Application)
    {
        // The signed-in user is whoever the test says is deciding. The handler
        // checks the participant is theirs, so this has to be settable.
        var caller = new TestCurrentUser { Id = asUser, Username = $"user-{asUser}" };

        var context = fixture.NewContext();

        var handler = new DecideApprovalHandler(
            context, NewResolver(), NewNotifications(context), fixture.Clock, caller,
            fixture.SqlErrors);

        return handler.HandleAsync(
            new DecideApprovalCommand(
                participantId, clientDecisionId ?? Guid.NewGuid(), approved, null, source),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchMyApprovalsResponse>> MyApprovalsAsync(
        int userId, bool pendingOnly = true)
    {
        var handler = new SearchMyApprovalsHandler(fixture.NewContext(), fixture.Clock);

        return handler.HandleAsync(
            new SearchMyApprovalsQuery(userId, pendingOnly, 0, 50),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<CancelApprovalResponse>> CancelAsync(int ticketId, string reason)
    {
        var context = fixture.NewContext();

        var handler = new CancelApprovalHandler(
            context, NewNotifications(context), fixture.Clock, fixture.CurrentUser);

        return handler.HandleAsync(
            new CancelApprovalCommand(ticketId, reason), TestContext.Current.CancellationToken);
    }

    private ApproverResolver NewResolver() => new(fixture.Users, fixture.Employees);

    /// <summary>
    /// Shares the caller's context, so the log rows it writes land in the same
    /// change tracker as the run they describe.
    /// </summary>
    private ApprovalNotifications NewNotifications(Persistence.ServiceDeskDbContext context) =>
        new(context, fixture.Notifier, fixture.Users, fixture.Clock);
}
