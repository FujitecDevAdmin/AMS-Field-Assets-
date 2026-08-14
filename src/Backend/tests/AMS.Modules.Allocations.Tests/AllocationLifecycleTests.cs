using AMS.Modules.Allocations.Domain;
using AMS.Modules.Allocations.Features.AllocateAsset;
using AMS.Modules.Allocations.Features.ApproveAcknowledgement;
using AMS.Modules.Allocations.Features.DecideAllocationRequest;
using AMS.Modules.Allocations.Features.GetMyAssets;
using AMS.Modules.Allocations.Features.ReceiveReturn;
using AMS.Modules.Allocations.Features.RequestAllocation;
using AMS.Modules.Allocations.Features.RequestReturn;
using AMS.Modules.Allocations.Features.ReverseReturn;
using AMS.Modules.Allocations.Features.SearchAllocationRequests;
using AMS.Modules.Allocations.Features.SearchAllocations;
using AMS.Modules.Allocations.Features.SignAcknowledgement;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Results;

namespace AMS.Modules.Allocations.Tests;

/// <summary>
/// Catalogue screens: Allocation Requests, Allocations, My Assets. The
/// lifecycle from "request an asset for an employee" to "reverse a return made
/// in error".
/// </summary>
[Collection(nameof(AllocationsCollectionDefinition))]
public sealed class AllocationLifecycleTests(AllocationsFixture fixture)
{
    private const int Alice = 100;
    private const int Bob = 200;

    // ------------------------------------------------------------- positive

    [Fact]
    public async Task An_asset_can_be_allocated_and_found()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0001");

        var result = await AllocateAsync(asset, Alice);

        result.IsSuccess.ShouldBeTrue();
        var page = (await SearchAsync()).Value;
        page.TotalCount.ShouldBe(1);
        page.Rows.Single().EmployeeId.ShouldBe(Alice);
    }

    [Fact]
    public async Task Allocating_writes_a_timeline_entry_in_the_same_transaction()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0002");

        await AllocateAsync(asset, Alice);

        (await fixture.TimelineOfAsync(asset)).ShouldBe(["Allocated"]);
    }

    [Fact]
    public async Task Allocating_creates_the_acknowledgement_as_pending()
    {
        // The row exists from the start, so "not signed yet" is a state rather
        // than a missing row the screen has to interpret.
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0003");

        await AllocateAsync(asset, Alice);

        (await SearchAsync()).Value.Rows.Single()
            .AcknowledgementStatus.ShouldBe(AcknowledgementStatus.Pending);
    }

    [Fact]
    public async Task Receiving_a_return_closes_it_and_frees_the_asset()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0004");
        var first = await AllocateAsync(asset, Alice);

        (await ReceiveReturnAsync(first.Value.Id)).IsSuccess.ShouldBeTrue();

        // Freed: the filtered index only covers open rows, so this now succeeds.
        (await AllocateAsync(asset, Bob)).IsSuccess.ShouldBeTrue();
        (await fixture.TimelineOfAsync(asset)).ShouldBe(["Allocated", "Returned", "Allocated"]);
    }

    [Fact]
    public async Task The_full_request_and_decision_path_works()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0005");

        var request = await RequestAsync(asset, Alice);
        request.Value.Status.ShouldBe(ApprovalStatus.Pending);

        var decided = await DecideAsync(request.Value.Id, approved: true);
        decided.Value.Status.ShouldBe(ApprovalStatus.Approved);

        var allocated = await AllocateAsync(asset, Alice, approvalId: request.Value.Id);
        allocated.IsSuccess.ShouldBeTrue();

        // The request now points at what it produced.
        (await SearchRequestsAsync()).Value.Rows.Single()
            .AllocationId.ShouldBe(allocated.Value.Id);
    }

    [Fact]
    public async Task A_rejected_request_keeps_the_remark_that_says_why()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0006");
        var request = await RequestAsync(asset, Alice);

        await DecideAsync(request.Value.Id, approved: false, remarks: "Already has one.");

        var row = (await SearchRequestsAsync()).Value.Rows.Single();
        row.Status.ShouldBe(ApprovalStatus.Rejected);
        row.DecisionRemarks.ShouldBe("Already has one.");
    }

    // ------------------------------------------------------------- negative

    [Fact]
    public async Task One_holder_at_a_time_is_enforced_by_the_database()
    {
        // No read-then-write check anywhere in the handler. This is 2601 on
        // UX_AssetAllocation_OneActivePerAsset, translated to a 409.
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0007");
        await AllocateAsync(asset, Alice);

        var second = await AllocateAsync(asset, Bob);

        second.IsSuccess.ShouldBeFalse();
        second.Error!.Code.ShouldBe("Allocation.AssetAlreadyIssued");
    }

    [Fact]
    public async Task A_closed_allocation_cannot_be_returned_again()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0008");
        var allocation = await AllocateAsync(asset, Alice);
        await ReceiveReturnAsync(allocation.Value.Id);

        var again = await ReceiveReturnAsync(allocation.Value.Id);

        again.IsSuccess.ShouldBeFalse();
        again.Error!.Code.ShouldBe("Allocation.AlreadyReturned");
    }

    [Fact]
    public async Task An_unknown_allocation_is_a_404()
    {
        await fixture.ResetAsync();

        (await ReceiveReturnAsync(987654)).Error!.Code.ShouldBe("Allocation.NotFound");
        (await ReverseAsync(987654, "typo")).Error!.Code.ShouldBe("Allocation.NotFound");
    }

    [Fact]
    public async Task A_request_cannot_be_decided_twice()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0009");
        var request = await RequestAsync(asset, Alice);
        await DecideAsync(request.Value.Id, approved: true);

        var again = await DecideAsync(request.Value.Id, approved: false);

        again.IsSuccess.ShouldBeFalse();
        again.Error!.Code.ShouldBe("AllocationRequest.AlreadyDecided");
    }

    [Fact]
    public async Task Two_pending_requests_for_one_asset_and_employee_are_refused()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0010");
        await RequestAsync(asset, Alice);

        var duplicate = await RequestAsync(asset, Alice);

        duplicate.IsSuccess.ShouldBeFalse();
        duplicate.Error!.Code.ShouldBe("AllocationRequest.AlreadyPending");
    }

    [Fact]
    public async Task An_undecided_request_cannot_be_acted_on()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0011");
        var request = await RequestAsync(asset, Alice);

        var result = await AllocateAsync(asset, Alice, approvalId: request.Value.Id);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("AllocationRequest.NotApproved");
    }

    [Fact]
    public async Task An_approved_request_cannot_be_acted_on_twice()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0012");
        var request = await RequestAsync(asset, Alice);
        await DecideAsync(request.Value.Id, approved: true);
        var first = await AllocateAsync(asset, Alice, approvalId: request.Value.Id);
        await ReceiveReturnAsync(first.Value.Id);

        var again = await AllocateAsync(asset, Alice, approvalId: request.Value.Id);

        again.IsSuccess.ShouldBeFalse();
        again.Error!.Code.ShouldBe("AllocationRequest.AlreadyActioned");
    }

    // ----------------------------------------------------------------- edge

    [Fact]
    public async Task A_reversed_return_puts_the_allocation_back()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0013");
        var allocation = await AllocateAsync(asset, Alice);
        await ReceiveReturnAsync(allocation.Value.Id);

        var reversal = await ReverseAsync(allocation.Value.Id, "Received the wrong asset.");

        reversal.IsSuccess.ShouldBeTrue();
        var row = (await SearchAsync()).Value.Rows.Single();
        row.ReturnedOnUtc.ShouldBeNull();
        row.EmployeeId.ShouldBe(Alice);
        (await fixture.TimelineOfAsync(asset))
            .ShouldBe(["Allocated", "Returned", "ReturnReversed"]);
    }

    [Fact]
    public async Task A_return_cannot_be_reversed_once_the_asset_is_reissued()
    {
        // Putting it back would mean two people holding one asset. The filtered
        // unique index says so; the handler does not have to.
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0014");
        var first = await AllocateAsync(asset, Alice);
        await ReceiveReturnAsync(first.Value.Id);
        await AllocateAsync(asset, Bob);

        var reversal = await ReverseAsync(first.Value.Id, "Mistake.");

        reversal.IsSuccess.ShouldBeFalse();
        reversal.Error!.Code.ShouldBe("Allocation.AssetAlreadyIssued");
    }

    [Fact]
    public async Task An_open_allocation_cannot_be_reversed()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0015");
        var allocation = await AllocateAsync(asset, Alice);

        var reversal = await ReverseAsync(allocation.Value.Id, "Nothing to undo.");

        reversal.IsSuccess.ShouldBeFalse();
        reversal.Error!.Code.ShouldBe("Allocation.NotReturned");
    }

    [Fact]
    public async Task Overdue_is_computed_from_the_expected_return_date()
    {
        await fixture.ResetAsync();
        var late = await fixture.AddAssetAsync("AST-0016");
        var soon = await fixture.AddAssetAsync("AST-0017");
        await AllocateAsync(late, Alice, expected: new DateOnly(2026, 8, 1));
        await AllocateAsync(soon, Bob, expected: new DateOnly(2026, 12, 31));

        var overdue = (await SearchAsync(overdueOnly: true)).Value;

        overdue.TotalCount.ShouldBe(1);
        overdue.Rows.Single().AssetId.ShouldBe(late);
        overdue.Rows.Single().IsOverdue.ShouldBeTrue();
    }

    [Fact]
    public async Task A_returned_allocation_is_never_overdue()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0018");
        var allocation = await AllocateAsync(asset, Alice, expected: new DateOnly(2026, 8, 1));
        await ReceiveReturnAsync(allocation.Value.Id);

        (await SearchAsync(overdueOnly: true, openOnly: false)).Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_branch_administrator_sees_their_own_branches_and_the_unplaced()
    {
        await fixture.ResetAsync();
        var mine = await fixture.AddAssetAsync("AST-0019");
        var theirs = await fixture.AddAssetAsync("AST-0020");
        var nowhere = await fixture.AddAssetAsync("AST-0021");
        await AllocateAsync(mine, Alice, locationId: 1);
        await AllocateAsync(theirs, Bob, locationId: 2);
        await AllocateAsync(nowhere, Alice);

        fixture.CurrentUser.HasAllBranches = false;
        fixture.CurrentUser.BranchIds = new HashSet<int> { 1 };

        var rows = (await SearchAsync()).Value.Rows.Select(r => r.AssetId).ToArray();
        rows.ShouldBe([mine, nowhere], ignoreOrder: true);
    }

    // -------------------------------------------------------- my assets

    [Fact]
    public async Task An_employee_sees_only_what_they_hold()
    {
        await fixture.ResetAsync();
        var hers = await fixture.AddAssetAsync("AST-0022");
        var his = await fixture.AddAssetAsync("AST-0023");
        await AllocateAsync(hers, Alice);
        await AllocateAsync(his, Bob);

        fixture.CurrentUser.EmployeeId = Alice;

        var mine = (await MyAssetsAsync()).Value.Rows;
        mine.Single().AssetId.ShouldBe(hers);
    }

    [Fact]
    public async Task A_login_with_no_employee_is_told_so_rather_than_shown_an_empty_list()
    {
        await fixture.ResetAsync();
        fixture.CurrentUser.EmployeeId = null;

        var result = await MyAssetsAsync();

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("MyAssets.NoEmployee");
    }

    [Fact]
    public async Task Requesting_a_return_is_idempotent_and_does_not_move_the_timestamp()
    {
        // The branch queue sorts on it, so re-asking must not jump the queue.
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0024");
        var allocation = await AllocateAsync(asset, Alice);
        fixture.CurrentUser.EmployeeId = Alice;

        var first = await RequestReturnAsync(allocation.Value.Id);
        fixture.Clock.Advance(TimeSpan.FromHours(2));
        var second = await RequestReturnAsync(allocation.Value.Id);

        second.IsSuccess.ShouldBeTrue();
        second.Value.ReturnRequestedOnUtc.ShouldBe(first.Value.ReturnRequestedOnUtc);
    }

    [Fact]
    public async Task An_employee_cannot_request_a_return_on_somebody_elses_asset()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0025");
        var allocation = await AllocateAsync(asset, Alice);
        fixture.CurrentUser.EmployeeId = Bob;

        var result = await RequestReturnAsync(allocation.Value.Id);

        // A 404 and not a 403: that an allocation exists is itself a disclosure.
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Allocation.NotFound");
    }

    // --------------------------------------------------- acknowledgement

    [Fact]
    public async Task The_holder_signs_and_the_manager_countersigns()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0026");
        var allocation = await AllocateAsync(asset, Alice);

        fixture.CurrentUser.EmployeeId = Alice;
        (await SignAsync(allocation.Value.Id)).Value.Status.ShouldBe(AcknowledgementStatus.Signed);

        fixture.CurrentUser.EmployeeId = Bob;
        (await ApproveAckAsync(allocation.Value.Id)).Value.Status
            .ShouldBe(AcknowledgementStatus.Approved);
    }

    [Fact]
    public async Task Nobody_can_sign_for_somebody_else()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0027");
        var allocation = await AllocateAsync(asset, Alice);
        fixture.CurrentUser.EmployeeId = Bob;

        (await SignAsync(allocation.Value.Id)).Error!.Code.ShouldBe("Allocation.NotFound");
    }

    [Fact]
    public async Task An_employee_cannot_countersign_their_own()
    {
        // A countersignature is a second person's word. One signature entered
        // twice is not that.
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0028");
        var allocation = await AllocateAsync(asset, Alice);
        fixture.CurrentUser.EmployeeId = Alice;
        await SignAsync(allocation.Value.Id);

        var result = await ApproveAckAsync(allocation.Value.Id);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Acknowledgement.SelfApproval");
    }

    [Fact]
    public async Task An_unsigned_acknowledgement_cannot_be_countersigned()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0029");
        var allocation = await AllocateAsync(asset, Alice);
        fixture.CurrentUser.EmployeeId = Bob;

        var result = await ApproveAckAsync(allocation.Value.Id);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Acknowledgement.NotSigned");
    }

    [Fact]
    public async Task Signing_twice_is_refused()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0030");
        var allocation = await AllocateAsync(asset, Alice);
        fixture.CurrentUser.EmployeeId = Alice;
        await SignAsync(allocation.Value.Id);

        (await SignAsync(allocation.Value.Id)).Error!.Code
            .ShouldBe("Acknowledgement.AlreadySigned");
    }

    // -------------------------------------------------------------- helpers

    private Task<Result<AllocateAssetResponse>> AllocateAsync(
        int assetId, int employeeId, int? locationId = null,
        DateOnly? expected = null, int? approvalId = null)
    {
        var context = fixture.NewContext();
        var assets = fixture.NewAssetsContext();
        var handler = new AllocateAssetHandler(
            context, new AssetTimeline(assets), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new AllocateAssetCommand(assetId, employeeId, locationId, expected, approvalId, null),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<ReceiveReturnResponse>> ReceiveReturnAsync(int id)
    {
        var context = fixture.NewContext();
        var assets = fixture.NewAssetsContext();
        var handler = new ReceiveReturnHandler(
            context, new AssetTimeline(assets), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new ReceiveReturnCommand(id, null, null), TestContext.Current.CancellationToken);
    }

    private Task<Result<ReverseReturnResponse>> ReverseAsync(int id, string reason)
    {
        var context = fixture.NewContext();
        var assets = fixture.NewAssetsContext();
        var handler = new ReverseReturnHandler(
            context, new AssetTimeline(assets), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new ReverseReturnCommand(id, reason), TestContext.Current.CancellationToken);
    }

    private Task<Result<RequestAllocationResponse>> RequestAsync(int assetId, int employeeId)
    {
        var handler = new RequestAllocationHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new RequestAllocationCommand(assetId, employeeId, null, null),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<DecideAllocationRequestResponse>> DecideAsync(
        int id, bool approved, string? remarks = null)
    {
        var handler = new DecideAllocationRequestHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new DecideAllocationRequestCommand(id, approved, remarks),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<RequestReturnResponse>> RequestReturnAsync(int id)
    {
        var handler = new RequestReturnHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new RequestReturnCommand(id), TestContext.Current.CancellationToken);
    }

    private Task<Result<SignAcknowledgementResponse>> SignAsync(int allocationId)
    {
        var handler = new SignAcknowledgementHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new SignAcknowledgementCommand(allocationId, null, null),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<ApproveAcknowledgementResponse>> ApproveAckAsync(int allocationId)
    {
        var handler = new ApproveAcknowledgementHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new ApproveAcknowledgementCommand(allocationId), TestContext.Current.CancellationToken);
    }

    private Task<Result<GetMyAssetsResponse>> MyAssetsAsync()
    {
        var handler = new GetMyAssetsHandler(fixture.NewContext(), fixture.CurrentUser);
        return handler.HandleAsync(new GetMyAssetsQuery(), TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchAllocationsResponse>> SearchAsync(
        bool openOnly = true, bool overdueOnly = false)
    {
        var handler = new SearchAllocationsHandler(
            fixture.NewContext(), fixture.CurrentUser, fixture.Clock);
        return handler.HandleAsync(
            new SearchAllocationsQuery(null, null, null, openOnly, overdueOnly, 0, 50),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchAllocationRequestsResponse>> SearchRequestsAsync()
    {
        var handler = new SearchAllocationRequestsHandler(fixture.NewContext());
        return handler.HandleAsync(
            new SearchAllocationRequestsQuery(null, null, 0, 50),
            TestContext.Current.CancellationToken);
    }
}
