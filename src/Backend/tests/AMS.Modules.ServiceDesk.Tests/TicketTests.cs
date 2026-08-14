using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Features.AddRequestAttachment;
using AMS.Modules.ServiceDesk.Features.AddRequestNote;
using AMS.Modules.ServiceDesk.Features.AssignServiceRequest;
using AMS.Modules.ServiceDesk.Features.ChangeRequestStatus;
using AMS.Modules.ServiceDesk.Features.CreateRequestCategory;
using AMS.Modules.ServiceDesk.Features.CreateRequestSubCategory;
using AMS.Modules.ServiceDesk.Features.CreateServiceTemplate;
using AMS.Modules.ServiceDesk.Features.CreateSupportTeam;
using AMS.Modules.ServiceDesk.Features.GetServiceRequest;
using AMS.Modules.ServiceDesk.Features.RaiseServiceRequest;
using AMS.Modules.ServiceDesk.Features.SearchMyRequests;
using AMS.Modules.ServiceDesk.Features.SearchRequestQueue;
using AMS.Modules.ServiceDesk.Features.SendRequestEmail;
using AMS.Modules.Notifications.PublicApi.Notifications;
using AMS.Modules.ServiceLevel.PublicApi.ServiceLevel;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Tests;

/// <summary>
/// The tickets themselves: raising one, finding it, working it, and the clock
/// underneath. Pass two of three.
/// </summary>
[Collection(nameof(ServiceDeskCollectionDefinition))]
public sealed class TicketTests(ServiceDeskFixture fixture)
{
    // ------------------------------------------------------------- raising

    [Fact]
    public async Task A_ticket_is_raised_open_and_numbered()
    {
        await fixture.ResetAsync();

        var raised = await RaiseAsync("Cannot print");

        raised.IsSuccess.ShouldBeTrue();
        raised.Value.RequestNumber.ShouldStartWith("TKT-");
        raised.Value.Status.ShouldBe("Open");
    }

    [Fact]
    public async Task Two_tickets_never_share_a_number()
    {
        // A sequence, not MAX+1. Two people raising at once must not collide
        // on UX_ServiceRequest_Number for a reason neither could act on.
        await fixture.ResetAsync();

        var first = await RaiseAsync("First");
        var second = await RaiseAsync("Second");

        second.Value.RequestNumber.ShouldNotBe(first.Value.RequestNumber);
    }

    [Fact]
    public async Task The_number_carries_the_year_it_was_raised()
    {
        await fixture.ResetAsync();

        var raised = await RaiseAsync("Dated");

        raised.Value.RequestNumber.ShouldStartWith($"TKT-{fixture.Clock.UtcNow.Year}-");
    }

    [Fact]
    public async Task A_kind_the_database_does_not_allow_is_refused()
    {
        // CK_ServiceRequest_Kind would reject it too, but as a 500. The
        // handler names the three that are allowed.
        await fixture.ResetAsync();

        var result = await RaiseAsync("Odd", kind: "Complaint");

        result.Error!.Code.ShouldBe("ServiceRequest.UnknownKind");
    }

    [Fact]
    public async Task A_priority_the_database_does_not_allow_is_refused()
    {
        await fixture.ResetAsync();

        (await RaiseAsync("Urgent", priority: "Immediate")).Error!.Code
            .ShouldBe("ServiceRequest.UnknownPriority");
    }

    [Fact]
    public async Task An_asset_issue_must_name_an_asset()
    {
        await fixture.ResetAsync();

        (await RaiseAsync("Something broke", kind: RequestKind.AssetIssue)).Error!.Code
            .ShouldBe("ServiceRequest.AssetRequired");
    }

    [Fact]
    public async Task An_asset_issue_may_describe_an_asset_that_is_not_on_the_register()
    {
        // The requester reads a sticker; the register may not have it yet.
        // Refusing the ticket would lose the fault as well as the asset.
        await fixture.ResetAsync();

        var result = await RaiseAsync(
            "Lift panel dead", kind: RequestKind.AssetIssue, manualAsset: "Panel by loading bay");

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task A_sub_category_from_another_category_is_refused()
    {
        // Two independent foreign keys; nothing in the schema stops a ticket
        // being classified two ways at once.
        await fixture.ResetAsync();
        var network = (await CreateCategoryAsync("Network")).Value.Id;
        var desktop = (await CreateCategoryAsync("Desktop")).Value.Id;
        var vpn = (await CreateSubCategoryAsync(network, "VPN")).Value.Id;

        var result = await RaiseAsync("Mixed up", categoryId: desktop, subCategoryId: vpn);

        result.Error!.Code.ShouldBe("ServiceRequest.SubCategoryMismatch");
    }

    [Fact]
    public async Task A_template_fills_in_what_the_form_left_blank()
    {
        await fixture.ResetAsync();
        var categoryId = (await CreateCategoryAsync("Access")).Value.Id;
        var teamId = (await CreateTeamAsync("Access Desk")).Value.Id;
        var templateId = (await CreateTemplateAsync(
            "New joiner", categoryId: categoryId, teamId: teamId, priority: RequestPriority.High))
            .Value.Id;

        var raised = await RaiseAsync("Joiner", templateId: templateId);

        var detail = (await GetAsync(raised.Value.Id)).Value;
        detail.RequestCategoryId.ShouldBe(categoryId);
        detail.AssignedTeamId.ShouldBe(teamId);
        detail.Priority.ShouldBe(RequestPriority.High);
    }

    [Fact]
    public async Task What_the_requester_typed_beats_the_template()
    {
        // A template that overrode the form would be a form that argues.
        await fixture.ResetAsync();
        var templateCategory = (await CreateCategoryAsync("Access")).Value.Id;
        var chosen = (await CreateCategoryAsync("Network")).Value.Id;
        var templateId = (await CreateTemplateAsync("New joiner", categoryId: templateCategory))
            .Value.Id;

        var raised = await RaiseAsync("Joiner", templateId: templateId, categoryId: chosen);

        (await GetAsync(raised.Value.Id)).Value.RequestCategoryId.ShouldBe(chosen);
    }

    [Fact]
    public async Task A_retired_template_cannot_be_used()
    {
        await fixture.ResetAsync();
        var templateId = (await CreateTemplateAsync("Old form")).Value.Id;
        await RetireTemplateAsync(templateId);

        (await RaiseAsync("Joiner", templateId: templateId)).Error!.Code
            .ShouldBe("ServiceTemplate.Retired");
    }

    [Fact]
    public async Task A_template_that_requires_an_asset_gets_one()
    {
        await fixture.ResetAsync();
        var templateId = (await CreateTemplateAsync("Laptop fault", requiresAsset: true)).Value.Id;

        (await RaiseAsync("Broken", templateId: templateId)).Error!.Code
            .ShouldBe("ServiceRequest.AssetRequired");
    }

    [Fact]
    public async Task A_new_service_request_carries_its_questions_and_its_kit()
    {
        await fixture.ResetAsync();

        var raised = await RaiseAsync(
            "New joiner: R Nair",
            kind: RequestKind.NewService,
            newService: new RaiseServiceRequestCommand.NewServiceDetail(
                NeedsEmail: true, NeedsErp: true, NeedsDms: false, NeedsVpn: false,
                RequiredByDate: new DateOnly(2026, 9, 1),
                Notes: "Starts on the first.",
                Items: [new RaiseServiceRequestCommand.NewServiceItem(4, 2, "16 GB")]));

        var detail = (await GetAsync(raised.Value.Id)).Value;
        detail.NewService.ShouldNotBeNull();
        detail.NewService.NeedsEmail.ShouldBeTrue();
        detail.NewService.RequiredByDate.ShouldBe(new DateOnly(2026, 9, 1));
        detail.NewService.Items.Single().Quantity.ShouldBe(2);
    }

    [Fact]
    public async Task A_new_service_request_without_its_questions_is_refused()
    {
        await fixture.ResetAsync();

        (await RaiseAsync("Joiner", kind: RequestKind.NewService)).Error!.Code
            .ShouldBe("ServiceRequest.NewServiceDetailRequired");
    }

    [Fact]
    public async Task New_service_questions_on_a_support_ticket_are_refused()
    {
        await fixture.ResetAsync();

        var result = await RaiseAsync(
            "Printer",
            newService: new RaiseServiceRequestCommand.NewServiceDetail(
                false, false, false, false, null, null, []));

        result.Error!.Code.ShouldBe("ServiceRequest.NewServiceDetailNotAllowed");
    }

    [Fact]
    public async Task A_line_of_kit_must_ask_for_at_least_one()
    {
        // CK_NewServiceRequestItem_PositiveQuantity, refused before it fires.
        await fixture.ResetAsync();

        var result = await RaiseAsync(
            "Joiner",
            kind: RequestKind.NewService,
            newService: new RaiseServiceRequestCommand.NewServiceDetail(
                false, false, false, false, null, null,
                [new RaiseServiceRequestCommand.NewServiceItem(4, 0, null)]));

        result.Error!.Code.ShouldBe("ServiceRequest.ItemQuantity");
    }

    [Fact]
    public async Task Raising_writes_the_first_line_of_the_timeline()
    {
        await fixture.ResetAsync();

        var raised = await RaiseAsync("Cannot print");

        var entry = (await GetAsync(raised.Value.Id)).Value.History.Single();
        entry.EntryKind.ShouldBe(HistoryEntryKind.Transition);
        entry.EntryText.ShouldContain(raised.Value.RequestNumber);
    }

    // ------------------------------------------------------------- reading

    [Fact]
    public async Task My_requests_are_mine_and_nobody_elses()
    {
        await fixture.ResetAsync();
        await RaiseAsync("Mine", employeeId: 7);
        await RaiseAsync("Theirs", employeeId: 8);

        var rows = (await MyRequestsAsync(7)).Value.Rows;

        rows.Single().Subject.ShouldBe("Mine");
    }

    [Fact]
    public async Task A_request_raised_for_me_is_also_mine()
    {
        // A manager raises the joiner request; the joiner has to be able to
        // watch it without knowing which column they are in.
        await fixture.ResetAsync();
        await RaiseAsync("For the joiner", employeeId: 7, onBehalfOfEmployeeId: 9);

        (await MyRequestsAsync(9)).Value.Rows.Single().Subject.ShouldBe("For the joiner");
    }

    [Fact]
    public async Task An_account_with_no_employee_record_is_told_so()
    {
        await fixture.ResetAsync();

        (await MyRequestsAsync(0)).Error!.Code.ShouldBe("ServiceRequest.NoEmployee");
    }

    [Fact]
    public async Task My_requests_can_hide_the_finished_ones()
    {
        await fixture.ResetAsync();
        var closing = await RaiseAsync("Done", employeeId: 7);
        await RaiseAsync("Still going", employeeId: 7);
        await ChangeStatusAsync(closing.Value.Id, "Closed", resolution: "Replaced the cable.");

        (await MyRequestsAsync(7, openOnly: true)).Value.Rows.Single().Subject
            .ShouldBe("Still going");
    }

    [Fact]
    public async Task The_queue_puts_overdue_tickets_first()
    {
        // The whole point of the screen, and of IX_ServiceRequest_SlaQueue.
        await fixture.ResetAsync();
        var fine = await RaiseAsync("Not late");
        var late = await RaiseAsync("Late");
        await MakeOverdueAsync(late.Value.Id);

        var rows = (await QueueAsync()).Value.Rows;

        rows[0].Subject.ShouldBe("Late");
        rows[0].IsSlaOverdue.ShouldBeTrue();
        rows[1].Id.ShouldBe(fine.Value.Id);
    }

    [Fact]
    public async Task A_ticket_with_no_due_date_sorts_after_one_that_has_one()
    {
        // SQL Server sorts NULL first. Left alone, every ticket with no policy
        // would float to the top of a queue ordered by urgency.
        await fixture.ResetAsync();
        var noPolicy = await RaiseAsync("No policy");
        var due = await RaiseAsync("Due Friday");
        await SetResolutionDueAsync(due.Value.Id, fixture.Clock.UtcNow.AddDays(3));

        var rows = (await QueueAsync()).Value.Rows;

        rows[0].Id.ShouldBe(due.Value.Id);
        rows[1].Id.ShouldBe(noPolicy.Value.Id);
    }

    [Fact]
    public async Task Equally_due_tickets_sort_by_priority()
    {
        await fixture.ResetAsync();
        await RaiseAsync("Low one", priority: RequestPriority.Low);
        await RaiseAsync("Critical one", priority: RequestPriority.Critical);

        (await QueueAsync()).Value.Rows[0].Subject.ShouldBe("Critical one");
    }

    [Fact]
    public async Task The_overdue_count_is_over_the_filter_not_the_page()
    {
        // "3 of 50 overdue" on a page holding none is a number nobody acts on.
        await fixture.ResetAsync();
        for (var i = 0; i < 3; i++)
        {
            var ticket = await RaiseAsync($"Late {i}");
            await MakeOverdueAsync(ticket.Value.Id);
        }

        await RaiseAsync("Fine");

        var page = (await QueueAsync(take: 1)).Value;

        page.Rows.Count.ShouldBe(1);
        page.TotalCount.ShouldBe(4);
        page.OverdueCount.ShouldBe(3);
    }

    [Fact]
    public async Task The_queue_hides_closed_tickets_unless_asked()
    {
        await fixture.ResetAsync();
        var closing = await RaiseAsync("Finished");
        await ChangeStatusAsync(closing.Value.Id, "Closed", resolution: "Done.");
        await RaiseAsync("Open one");

        (await QueueAsync()).Value.Rows.Single().Subject.ShouldBe("Open one");
        (await QueueAsync(openOnly: false)).Value.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public async Task The_queue_can_show_what_nobody_has_picked_up()
    {
        await fixture.ResetAsync();
        var held = await RaiseAsync("Somebody has this");
        await RaiseAsync("Nobody has this");
        await AssignAsync(held.Value.Id, userId: 5);

        (await QueueAsync(unassigned: true)).Value.Rows.Single().Subject
            .ShouldBe("Nobody has this");
    }

    [Fact]
    public async Task The_queue_searches_by_number_and_by_subject()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Printer jam on the third floor");
        await RaiseAsync("VPN drops");

        (await QueueAsync(search: "Printer")).Value.Rows.Single().Id.ShouldBe(raised.Value.Id);
        (await QueueAsync(search: raised.Value.RequestNumber)).Value.Rows.Single().Id
            .ShouldBe(raised.Value.Id);
    }

    [Fact]
    public async Task A_ticket_that_does_not_exist_is_a_not_found()
    {
        await fixture.ResetAsync();

        (await GetAsync(987654)).Error!.Code.ShouldBe("ServiceRequest.NotFound");
    }

    [Fact]
    public async Task Internal_notes_are_hidden_from_the_requester_view()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");
        await AddNoteAsync(raised.Value.Id, "Rang the user.", isInternal: false);
        await AddNoteAsync(raised.Value.Id, "User is being difficult.", isInternal: true);

        var requesterView = (await GetAsync(raised.Value.Id)).Value;
        var technicianView = (await GetAsync(raised.Value.Id, includeInternal: true)).Value;

        requesterView.History.ShouldNotContain(h => h.IsInternal);
        technicianView.History.Count(h => h.EntryKind == HistoryEntryKind.Note).ShouldBe(2);
    }

    // ---------------------------------------------------------- assignment

    [Fact]
    public async Task Assigning_a_new_ticket_moves_it_on()
    {
        // A ticket somebody holds is not still waiting to be looked at.
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        var assigned = await AssignAsync(raised.Value.Id, userId: 5);

        assigned.Value.AssignedToUserId.ShouldBe(5);
        assigned.Value.StatusName.ShouldBe("Assigned");
    }

    [Fact]
    public async Task Assignment_must_name_somebody()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        (await AssignAsync(raised.Value.Id)).Error!.Code
            .ShouldBe("ServiceRequest.AssigneeRequired");
    }

    [Fact]
    public async Task A_ticket_cannot_be_given_to_a_retired_team()
    {
        await fixture.ResetAsync();
        var teamId = (await CreateTeamAsync("Old desk")).Value.Id;
        await RetireTeamAsync(teamId, "Old desk");
        var raised = await RaiseAsync("Cannot print");

        (await AssignAsync(raised.Value.Id, teamId: teamId)).Error!.Code
            .ShouldBe("SupportTeam.Retired");
    }

    [Fact]
    public async Task A_ticket_cannot_be_given_to_a_team_that_does_not_exist()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        (await AssignAsync(raised.Value.Id, teamId: 987654)).Error!.Code
            .ShouldBe("SupportTeam.NotFound");
    }

    [Fact]
    public async Task Assigning_to_a_team_alone_leaves_the_ticket_where_it_is()
    {
        // A team is not a person. Until somebody picks it up it is still
        // waiting to be looked at.
        await fixture.ResetAsync();
        var teamId = (await CreateTeamAsync("North desk")).Value.Id;
        var raised = await RaiseAsync("Cannot print");

        var assigned = await AssignAsync(raised.Value.Id, teamId: teamId);

        assigned.Value.StatusName.ShouldBe("Open");
        assigned.Value.AssignedTeamId.ShouldBe(teamId);
    }

    // ------------------------------------------------------------ statuses

    [Fact]
    public async Task A_ticket_moves_through_the_statuses()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        var moved = await ChangeStatusAsync(raised.Value.Id, "In Progress");

        moved.Value.StatusName.ShouldBe("In Progress");
        moved.Value.IsClosedState.ShouldBeFalse();
    }

    [Fact]
    public async Task A_ticket_cannot_move_to_where_it_already_is()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        (await ChangeStatusAsync(raised.Value.Id, "Open")).Error!.Code
            .ShouldBe("ServiceRequest.SameStatus");
    }

    [Fact]
    public async Task Finishing_a_ticket_needs_the_resolution_written_down()
    {
        // The only thing standing between an SLA report and a column of blanks.
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        (await ChangeStatusAsync(raised.Value.Id, "Resolved")).Error!.Code
            .ShouldBe("ServiceRequest.ResolutionRequired");
    }

    [Fact]
    public async Task Closing_a_resolved_ticket_keeps_the_resolution_it_already_has()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");
        await ChangeStatusAsync(raised.Value.Id, "Resolved", resolution: "New toner.");

        var closed = await ChangeStatusAsync(raised.Value.Id, "Closed");

        closed.IsSuccess.ShouldBeTrue();
        (await GetAsync(raised.Value.Id)).Value.Resolution.ShouldBe("New toner.");
    }

    [Fact]
    public async Task Closing_stamps_the_closing_time()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        await ChangeStatusAsync(raised.Value.Id, "Closed", resolution: "New toner.");

        var detail = (await GetAsync(raised.Value.Id)).Value;
        detail.ClosedOnUtc.ShouldNotBeNull();
        detail.ResolvedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task Reopening_undoes_the_closure()
    {
        // A row with a closing time and an open status is a row every report
        // has to special-case.
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");
        await ChangeStatusAsync(raised.Value.Id, "Closed", resolution: "New toner.");

        var reopened = await ChangeStatusAsync(raised.Value.Id, "In Progress");

        reopened.IsSuccess.ShouldBeTrue();
        var detail = (await GetAsync(raised.Value.Id)).Value;
        detail.ClosedOnUtc.ShouldBeNull();
        detail.ResolvedOnUtc.ShouldBeNull();
        detail.History.ShouldContain(h => h.EntryText.StartsWith("Reopened", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_status_that_does_not_exist_is_a_not_found()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        var handler = NewChangeStatusHandler();
        var result = await handler.HandleAsync(
            new ChangeRequestStatusCommand(raised.Value.Id, 987654, null, null),
            TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("RequestStatus.NotFound");
    }

    [Fact]
    public async Task A_retired_status_cannot_be_moved_to()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");
        await fixture.ExecuteAsync(
            "UPDATE [ServiceDesk].[RequestStatus] SET [IsActive] = 0 WHERE [StatusName] = N'Standby Provided';");

        var result = await ChangeStatusAsync(raised.Value.Id, "Standby Provided");

        await fixture.ExecuteAsync(
            "UPDATE [ServiceDesk].[RequestStatus] SET [IsActive] = 1 WHERE [StatusName] = N'Standby Provided';");
        result.Error!.Code.ShouldBe("RequestStatus.Retired");
    }

    // --------------------------------------------------------- the clock

    [Fact]
    public async Task Time_spent_in_a_running_status_is_charged()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        fixture.Clock.Advance(TimeSpan.FromMinutes(30));
        var moved = await ChangeStatusAsync(raised.Value.Id, "In Progress");

        moved.Value.ResolutionConsumedMinutes.ShouldBe(30);
    }

    [Fact]
    public async Task Time_spent_on_hold_consumes_nothing()
    {
        // The design script's own words: a ticket held over a weekend consumes
        // nothing. Operational minutes, not wall clock.
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Waiting on the user");
        await ChangeStatusAsync(raised.Value.Id, "On Hold");

        fixture.Clock.Advance(TimeSpan.FromDays(2));
        var resumed = await ChangeStatusAsync(raised.Value.Id, "In Progress");

        resumed.Value.ResolutionConsumedMinutes.ShouldBe(0);
        resumed.Value.IsSlaPaused.ShouldBeFalse();
    }

    [Fact]
    public async Task A_paused_status_says_so()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Waiting on a spare");

        var held = await ChangeStatusAsync(raised.Value.Id, "Waiting for Spare");

        held.Value.IsSlaPaused.ShouldBeTrue();
    }

    [Fact]
    public async Task Technician_time_is_counted_separately_from_the_sla_clock()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");
        await ChangeStatusAsync(raised.Value.Id, "In Progress");

        fixture.Clock.Advance(TimeSpan.FromMinutes(45));
        await ChangeStatusAsync(raised.Value.Id, "On Hold");

        await using var db = fixture.NewContext();
        var ticket = await db.ServiceRequests.SingleAsync(
            r => r.Id == raised.Value.Id, TestContext.Current.CancellationToken);
        ticket.TechnicianWorkingMinutes.ShouldBe(45);
        ticket.ResolutionConsumedMinutes.ShouldBe(45);
    }

    [Fact]
    public async Task Time_after_a_ticket_is_resolved_belongs_to_nobody()
    {
        // Otherwise reopening a ticket would retrospectively blow an SLA it met.
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");
        await ChangeStatusAsync(raised.Value.Id, "Resolved", resolution: "New toner.");
        var consumedAtResolution =
            (await GetAsync(raised.Value.Id)).Value.ResolutionConsumedMinutes;

        fixture.Clock.Advance(TimeSpan.FromDays(5));
        var reopened = await ChangeStatusAsync(raised.Value.Id, "In Progress");

        reopened.Value.ResolutionConsumedMinutes.ShouldBe(consumedAtResolution);
    }

    [Fact]
    public async Task A_resolved_ticket_is_never_overdue()
    {
        // It does not become late tonight because its due date passed.
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");
        await SetResolutionDueAsync(raised.Value.Id, fixture.Clock.UtcNow.AddHours(1));

        fixture.Clock.Advance(TimeSpan.FromHours(4));
        await ChangeStatusAsync(raised.Value.Id, "Resolved", resolution: "Fixed.");

        (await GetAsync(raised.Value.Id)).Value.IsSlaOverdue.ShouldBeFalse();
    }

    [Fact]
    public async Task Blowing_the_due_date_marks_the_ticket_overdue()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");
        await SetResolutionDueAsync(raised.Value.Id, fixture.Clock.UtcNow.AddHours(1));

        fixture.Clock.Advance(TimeSpan.FromHours(4));
        await ChangeStatusAsync(raised.Value.Id, "In Progress");

        (await GetAsync(raised.Value.Id)).Value.IsSlaOverdue.ShouldBeTrue();
    }

    [Fact]
    public async Task The_first_response_is_stamped_once_and_never_again()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        fixture.Clock.Advance(TimeSpan.FromMinutes(10));
        await AddNoteAsync(raised.Value.Id, "Rang the user.");
        var first = (await GetAsync(raised.Value.Id)).Value.FirstResponseOnUtc;

        fixture.Clock.Advance(TimeSpan.FromMinutes(10));
        await AddNoteAsync(raised.Value.Id, "Rang again.");

        (await GetAsync(raised.Value.Id)).Value.FirstResponseOnUtc.ShouldBe(first);
    }

    [Fact]
    public async Task An_internal_note_is_not_a_response_to_the_requester()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        await AddNoteAsync(raised.Value.Id, "Looks like the fuser.", isInternal: true);

        (await GetAsync(raised.Value.Id, includeInternal: true)).Value.FirstResponseOnUtc
            .ShouldBeNull();
    }

    // ------------------------------------------------- what ServiceLevel says

    [Fact]
    public async Task A_ticket_takes_the_due_dates_the_policy_gives_it()
    {
        await fixture.ResetAsync();
        var start = fixture.Clock.UtcNow;
        fixture.Sla.Returns(new SlaTargets(
            7, "Medium priority", start, start.AddHours(1), start.AddHours(8), false, null));

        var raised = await RaiseAsync("Cannot print");

        var detail = (await GetAsync(raised.Value.Id)).Value;
        detail.ResponseDueOnUtc.ShouldBe(start.AddHours(1));
        detail.ResolutionDueOnUtc.ShouldBe(start.AddHours(8));
    }

    [Fact]
    public async Task A_site_with_no_policy_still_raises_tickets()
    {
        // Null targets are an ordinary answer, not a failure. The ticket simply
        // has no due date, and a ticket with no due date is never overdue.
        await fixture.ResetAsync();

        var raised = await RaiseAsync("Cannot print");

        raised.IsSuccess.ShouldBeTrue();
        var detail = (await GetAsync(raised.Value.Id)).Value;
        detail.ResolutionDueOnUtc.ShouldBeNull();
        detail.IsSlaOverdue.ShouldBeFalse();
    }

    [Fact]
    public async Task A_ticket_raised_out_of_hours_says_when_its_clock_starts()
    {
        // The requester's first question about a ticket nobody is working on.
        await fixture.ResetAsync();
        var raised_at = fixture.Clock.UtcNow;
        var opens = raised_at.AddHours(11);
        fixture.Sla.Returns(new SlaTargets(
            7, "Medium priority", opens, opens.AddHours(1), opens.AddHours(8),
            IsScheduledHold: true,
            ScheduleHoldReason: "Raised when the branch was closed. The clock starts on Monday at 09:00."));

        var raised = await RaiseAsync("Cannot print");

        var detail = (await GetAsync(raised.Value.Id)).Value;
        detail.History.ShouldContain(h => h.EntryKind == HistoryEntryKind.Sla);

        await using var db = fixture.NewContext();
        var ticket = await db.ServiceRequests.SingleAsync(
            r => r.Id == raised.Value.Id, TestContext.Current.CancellationToken);
        ticket.IsScheduledHold.ShouldBeTrue();
        // CK_ServiceRequest_ScheduledHold requires this whenever the flag is set.
        ticket.NextOperationalStartUtc.ShouldBe(opens);
        ticket.SlaStartOnUtc.ShouldBe(opens);
    }

    [Fact]
    public async Task The_clock_charges_operational_minutes_not_wall_clock()
    {
        // Two days of wall clock over a weekend is nothing at all, and the
        // calendar that knows why belongs to ServiceLevel.
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        fixture.Sla.Measures(0);
        fixture.Clock.Advance(TimeSpan.FromDays(2));
        var moved = await ChangeStatusAsync(raised.Value.Id, "In Progress");

        moved.Value.ResolutionConsumedMinutes.ShouldBe(0);
    }

    [Fact]
    public async Task A_span_the_calendar_counts_is_charged_in_full()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        fixture.Sla.Measures(240);
        fixture.Clock.Advance(TimeSpan.FromDays(3));
        var moved = await ChangeStatusAsync(raised.Value.Id, "In Progress");

        moved.Value.ResolutionConsumedMinutes.ShouldBe(240);
    }

    // ------------------------------------------------ notes, mail, files

    [Fact]
    public async Task A_note_lands_in_the_timeline_with_its_full_text()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");
        var longNote = new string('x', 900);

        var added = await AddNoteAsync(raised.Value.Id, longNote);

        added.IsSuccess.ShouldBeTrue();
        var entry = (await GetAsync(raised.Value.Id)).Value.History
            .Single(h => h.EntryKind == HistoryEntryKind.Note);
        entry.Body.ShouldBe(longNote);
        entry.EntryText.Length.ShouldBeLessThanOrEqualTo(200);
    }

    [Fact]
    public async Task An_e_mail_is_queued_and_appears_in_the_timeline()
    {
        // Queued, not Sent: delivery is the Notifications module's job, and
        // SMTP acceptance would not be inbox placement anyway.
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        var sent = await SendEmailAsync(raised.Value.Id, "user@fujitec.co.in");

        sent.Value.Status.ShouldBe(EmailStatus.Queued);
        var entry = (await GetAsync(raised.Value.Id)).Value.History
            .Single(h => h.EntryKind == HistoryEntryKind.Email);
        entry.RequestEmailId.ShouldBe(sent.Value.Id);
    }

    [Fact]
    public async Task An_e_mail_is_handed_to_the_outbox()
    {
        // The only thing in this system that talks to a mail server. Sending
        // inline would lose the message when SMTP is down, and nobody would
        // find out.
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        var sent = await SendEmailAsync(raised.Value.Id, "user@fujitec.co.in");

        var queued = fixture.Notifier.Queued.Single();
        queued.ToAddress.ShouldBe("user@fujitec.co.in");
        queued.SourceType.ShouldBe(EmailSource.ServiceRequest);
        queued.SourceId.ShouldBe(raised.Value.Id);

        await using var db = fixture.NewContext();
        var message = await db.RequestEmails.SingleAsync(
            e => e.Id == sent.Value.Id, TestContext.Current.CancellationToken);
        // The join between the ticket's copy of the conversation and the
        // delivery attempt.
        message.EmailOutboxId.ShouldNotBeNull();
    }

    [Fact]
    public async Task An_outbound_e_mail_records_who_sent_it()
    {
        // CK_RequestEmail_SentBy demands it, and it is the honest answer.
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        var sent = await SendEmailAsync(raised.Value.Id, "user@fujitec.co.in");

        await using var db = fixture.NewContext();
        var message = await db.RequestEmails.SingleAsync(
            e => e.Id == sent.Value.Id, TestContext.Current.CancellationToken);
        message.SentByUserId.ShouldBe(fixture.CurrentUser.Id);
        message.Direction.ShouldBe(EmailDirection.Outbound);
    }

    [Fact]
    public async Task A_file_is_recorded_against_the_ticket()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        var added = await AddAttachmentAsync(raised.Value.Id, @"\\files\tickets\error.png");

        added.IsSuccess.ShouldBeTrue();
        (await GetAsync(raised.Value.Id)).Value.Attachments.Single().FileName
            .ShouldBe("error.png");
    }

    [Fact]
    public async Task A_file_type_the_database_does_not_allow_is_refused()
    {
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");

        (await AddAttachmentAsync(raised.Value.Id, @"\\files\x.png", type: "Screenshot")).Error!.Code
            .ShouldBe("RequestAttachment.UnknownType");
    }

    [Fact]
    public async Task A_closed_ticket_takes_nothing_new()
    {
        // Reopen it. A ticket that keeps accumulating activity after it closed
        // has a life outside its own recorded lifetime, and two runs of the
        // same monthly report then disagree.
        await fixture.ResetAsync();
        var raised = await RaiseAsync("Cannot print");
        var id = raised.Value.Id;
        await ChangeStatusAsync(id, "Closed", resolution: "New toner.");

        (await AddNoteAsync(id, "One more thing.")).Error!.Code.ShouldBe("ServiceRequest.Closed");
        (await SendEmailAsync(id, "user@fujitec.co.in")).Error!.Code.ShouldBe("ServiceRequest.Closed");
        (await AddAttachmentAsync(id, @"\\files\x.png")).Error!.Code.ShouldBe("ServiceRequest.Closed");
        (await AssignAsync(id, userId: 5)).Error!.Code.ShouldBe("ServiceRequest.Closed");
    }

    [Fact]
    public async Task Notes_e_mails_and_files_need_a_ticket_that_exists()
    {
        await fixture.ResetAsync();

        (await AddNoteAsync(987654, "Hello")).Error!.Code.ShouldBe("ServiceRequest.NotFound");
        (await SendEmailAsync(987654, "user@fujitec.co.in")).Error!.Code
            .ShouldBe("ServiceRequest.NotFound");
        (await AddAttachmentAsync(987654, @"\\files\x.png")).Error!.Code
            .ShouldBe("ServiceRequest.NotFound");
        (await AssignAsync(987654, userId: 5)).Error!.Code.ShouldBe("ServiceRequest.NotFound");
    }

    // --------------------------------------------------------------- plumbing

    private Task<Result<RaiseServiceRequestResponse>> RaiseAsync(
        string subject,
        string kind = RequestKind.SupportTicket,
        string priority = RequestPriority.Medium,
        int? categoryId = null,
        int? subCategoryId = null,
        int? templateId = null,
        string? manualAsset = null,
        int employeeId = 1,
        int? onBehalfOfEmployeeId = null,
        RaiseServiceRequestCommand.NewServiceDetail? newService = null)
    {
        var handler = new RaiseServiceRequestHandler(
            fixture.NewContext(), fixture.Sla, fixture.Clock, fixture.CurrentUser,
            fixture.SqlErrors);

        return handler.HandleAsync(
            new RaiseServiceRequestCommand(
                kind, subject, null, priority, categoryId, subCategoryId, templateId,
                null, manualAsset, employeeId, onBehalfOfEmployeeId, null, newService),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<GetServiceRequestResponse>> GetAsync(int id, bool includeInternal = false)
    {
        var handler = new GetServiceRequestHandler(fixture.NewContext());

        return handler.HandleAsync(
            new GetServiceRequestQuery(id, includeInternal), TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchMyRequestsResponse>> MyRequestsAsync(
        int employeeId, bool openOnly = false)
    {
        var handler = new SearchMyRequestsHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchMyRequestsQuery(employeeId, openOnly, 0, 50),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchRequestQueueResponse>> QueueAsync(
        bool openOnly = true,
        bool unassigned = false,
        string? search = null,
        int take = 50)
    {
        var handler = new SearchRequestQueueHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchRequestQueueQuery(
                null, null, null, null, null, null, unassigned, false, openOnly, search, 0, take),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<AssignServiceRequestResponse>> AssignAsync(
        int id, int? userId = null, int? teamId = null)
    {
        var handler = new AssignServiceRequestHandler(
            fixture.NewContext(), fixture.Sla, fixture.Clock, fixture.CurrentUser);

        return handler.HandleAsync(
            new AssignServiceRequestCommand(id, userId, teamId, null),
            TestContext.Current.CancellationToken);
    }

    private ChangeRequestStatusHandler NewChangeStatusHandler() =>
        new(fixture.NewContext(), fixture.Sla, fixture.Clock, fixture.CurrentUser);

    private async Task<Result<ChangeRequestStatusResponse>> ChangeStatusAsync(
        int id, string statusName, string? resolution = null)
    {
        var statusId = await fixture.StatusIdAsync(statusName);

        return await NewChangeStatusHandler().HandleAsync(
            new ChangeRequestStatusCommand(id, statusId, resolution, null),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<AddRequestNoteResponse>> AddNoteAsync(
        int id, string note, bool isInternal = false)
    {
        var handler = new AddRequestNoteHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser);

        return handler.HandleAsync(
            new AddRequestNoteCommand(id, note, isInternal), TestContext.Current.CancellationToken);
    }

    private Task<Result<SendRequestEmailResponse>> SendEmailAsync(int id, string to)
    {
        var handler = new SendRequestEmailHandler(
            fixture.NewContext(), fixture.Notifier, fixture.Clock, fixture.CurrentUser);

        return handler.HandleAsync(
            new SendRequestEmailCommand(id, to, null, "Your ticket", "We are on it.", true),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<AddRequestAttachmentResponse>> AddAttachmentAsync(
        int id, string path, string type = AttachmentKind.Requester)
    {
        var handler = new AddRequestAttachmentHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser);

        return handler.HandleAsync(
            new AddRequestAttachmentCommand(
                id, type, path, Path.GetFileName(path), "image/png", 1024),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<CreateRequestCategoryResponse>> CreateCategoryAsync(string name)
    {
        var handler = new CreateRequestCategoryHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new CreateRequestCategoryCommand(name), TestContext.Current.CancellationToken);
    }

    private Task<Result<CreateRequestSubCategoryResponse>> CreateSubCategoryAsync(
        int categoryId, string name)
    {
        var handler = new CreateRequestSubCategoryHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new CreateRequestSubCategoryCommand(categoryId, name),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<CreateSupportTeamResponse>> CreateTeamAsync(string name)
    {
        var handler = new CreateSupportTeamHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new CreateSupportTeamCommand(name, null, null, false),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<Features.UpdateSupportTeam.UpdateSupportTeamResponse>> RetireTeamAsync(
        int id, string name)
    {
        var handler = new Features.UpdateSupportTeam.UpdateSupportTeamHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new Features.UpdateSupportTeam.UpdateSupportTeamCommand(id, name, null, null, false, false),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<CreateServiceTemplateResponse>> CreateTemplateAsync(
        string name,
        int? categoryId = null,
        int? teamId = null,
        string priority = RequestPriority.Medium,
        bool requiresAsset = false)
    {
        var handler = new CreateServiceTemplateHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new CreateServiceTemplateCommand(
                name, RequestKind.SupportTicket, categoryId, null, priority, teamId,
                SubjectTemplate: name, null, requiresAsset, 0),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<Features.UpdateServiceTemplate.UpdateServiceTemplateResponse>>
        RetireTemplateAsync(int id)
    {
        var handler = new Features.UpdateServiceTemplate.UpdateServiceTemplateHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new Features.UpdateServiceTemplate.UpdateServiceTemplateCommand(
                id, "Old form", null, null, RequestPriority.Medium, null,
                SubjectTemplate: "Old form", null, false, 0, IsActive: false),
            TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Gives a ticket a due date. The ServiceLevel module will compute these
    /// from a policy; until it ships the queue still has to be able to sort on
    /// them, so the tests set them directly.
    /// </summary>
    private Task SetResolutionDueAsync(int id, DateTime dueOnUtc) => fixture.ExecuteAsync(
        $"UPDATE [ServiceDesk].[ServiceRequest] SET [ResolutionDueOnUtc] = '{dueOnUtc:O}' WHERE [Id] = {id};");

    private Task MakeOverdueAsync(int id) => fixture.ExecuteAsync(
        $"UPDATE [ServiceDesk].[ServiceRequest] SET [IsSlaOverdue] = 1 WHERE [Id] = {id};");
}
