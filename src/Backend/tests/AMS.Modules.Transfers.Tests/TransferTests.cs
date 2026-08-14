using AMS.Modules.Assets.Persistence;
using AMS.Modules.Transfers.Domain;
using AMS.Modules.Transfers.Features.CancelTransfer;
using AMS.Modules.Transfers.Features.CompleteTransfer;
using AMS.Modules.Transfers.Features.DecideTransfer;
using AMS.Modules.Transfers.Features.RaiseTransfer;
using AMS.Modules.Transfers.Features.SearchTransferRequests;
using AMS.SharedKernel.Results;

namespace AMS.Modules.Transfers.Tests;

/// <summary>
/// Catalogue screen: Transfer Requests. A transfer is the approval and the
/// accounting consequence; approving says yes and completing makes it true.
/// </summary>
[Collection(nameof(TransfersCollectionDefinition))]
public sealed class TransferTests(TransfersFixture fixture)
{
    private const int Alice = 100;
    private const int Bob = 200;
    private const int Chennai = 1;
    private const int Bangalore = 2;

    // ------------------------------------------------------------- positive

    [Fact]
    public async Task A_transfer_can_be_raised_and_found()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0001", employeeId: Alice);

        var raised = await RaiseAsync(asset, TransferType.Employee, toEmployeeId: Bob);

        raised.IsSuccess.ShouldBeTrue();
        raised.Value.Status.ShouldBe(TransferStatus.Pending);
        (await SearchAsync()).Value.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_from_side_is_captured_from_the_asset_not_from_the_caller()
    {
        // A form that let somebody type where an asset came from is a form that
        // lets them record a move that never happened.
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync(
            "AST-0002", employeeId: Alice, locationId: Chennai,
            departmentId: 7, costCenter: "CC-100");

        await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);

        var row = (await SearchAsync()).Value.Rows.Single();
        row.FromEmployeeId.ShouldBe(Alice);
        row.FromLocationId.ShouldBe(Chennai);
        row.FromDepartmentId.ShouldBe(7);
        row.FromCostCenter.ShouldBe("CC-100");
    }

    [Fact]
    public async Task Approving_does_not_apply_anything()
    {
        // Approving says yes; completing makes it true. That gap is the whole
        // reason the two capabilities are separate.
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0003", locationId: Chennai);
        var transfer = await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);

        await DecideAsync(transfer.Value.Id, approved: true);

        (await fixture.CustodyOfAsync(asset)).Location.ShouldBe(Chennai);
    }

    [Fact]
    public async Task Completing_applies_the_change_and_writes_the_timeline()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0004", locationId: Chennai);
        var transfer = await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);
        await DecideAsync(transfer.Value.Id, approved: true);

        var completed = await CompleteAsync(transfer.Value.Id);

        completed.IsSuccess.ShouldBeTrue();
        completed.Value.Status.ShouldBe(TransferStatus.Completed);
        (await fixture.CustodyOfAsync(asset)).Location.ShouldBe(Bangalore);
        (await fixture.TimelineOfAsync(asset)).ShouldBe(["Transferred"]);
    }

    [Fact]
    public async Task Each_type_changes_only_its_own_column()
    {
        // A cost-centre transfer that also restated who holds the asset would
        // silently undo an allocation made while it sat in the queue.
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync(
            "AST-0005", employeeId: Alice, locationId: Chennai,
            departmentId: 7, costCenter: "CC-100");

        var transfer = await RaiseAsync(asset, TransferType.CostCenter, toCostCenter: "CC-200");
        await DecideAsync(transfer.Value.Id, approved: true);
        await CompleteAsync(transfer.Value.Id);

        var custody = await fixture.CustodyOfAsync(asset);
        custody.CostCenter.ShouldBe("CC-200");
        custody.Employee.ShouldBe(Alice, "an unrelated column must not move");
        custody.Location.ShouldBe(Chennai);
        custody.Department.ShouldBe(7);
    }

    [Fact]
    public async Task An_employee_transfer_moves_the_holder()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0006", employeeId: Alice);
        var transfer = await RaiseAsync(asset, TransferType.Employee, toEmployeeId: Bob);
        await DecideAsync(transfer.Value.Id, approved: true);

        await CompleteAsync(transfer.Value.Id);

        (await fixture.CustodyOfAsync(asset)).Employee.ShouldBe(Bob);
    }

    [Fact]
    public async Task A_rejected_transfer_keeps_the_remark()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0007", locationId: Chennai);
        var transfer = await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);

        await DecideAsync(transfer.Value.Id, approved: false, remarks: "Branch is closing.");

        var row = (await SearchAsync()).Value.Rows.Single();
        row.Status.ShouldBe(TransferStatus.Rejected);
        row.Remarks.ShouldBe("Branch is closing.");
    }

    // ------------------------------------------------------------ SAP queue

    [Fact]
    public async Task Branch_and_cost_centre_moves_are_queued_to_SAP()
    {
        await fixture.ResetAsync();
        var branch = await fixture.AddAssetAsync("AST-0008", locationId: Chennai);
        var cost = await fixture.AddAssetAsync("AST-0009", costCenter: "CC-100");

        var a = await RaiseAsync(branch, TransferType.Branch, toLocationId: Bangalore);
        await DecideAsync(a.Value.Id, approved: true);
        (await CompleteAsync(a.Value.Id)).Value.SapSyncStatus.ShouldBe(SapSyncStatus.Pending);

        var b = await RaiseAsync(cost, TransferType.CostCenter, toCostCenter: "CC-200");
        await DecideAsync(b.Value.Id, approved: true);
        (await CompleteAsync(b.Value.Id)).Value.SapSyncStatus.ShouldBe(SapSyncStatus.Pending);
    }

    [Fact]
    public async Task Employee_and_department_moves_are_not()
    {
        // AMS's own bookkeeping. Queueing them would put thousands of rows in
        // front of a system that discards them.
        await fixture.ResetAsync();
        var employee = await fixture.AddAssetAsync("AST-0010", employeeId: Alice);
        var department = await fixture.AddAssetAsync("AST-0011", departmentId: 7);

        var a = await RaiseAsync(employee, TransferType.Employee, toEmployeeId: Bob);
        await DecideAsync(a.Value.Id, approved: true);
        (await CompleteAsync(a.Value.Id)).Value.SapSyncStatus.ShouldBe(SapSyncStatus.NotRequired);

        var b = await RaiseAsync(department, TransferType.Department, toDepartmentId: 9);
        await DecideAsync(b.Value.Id, approved: true);
        (await CompleteAsync(b.Value.Id)).Value.SapSyncStatus.ShouldBe(SapSyncStatus.NotRequired);
    }

    [Fact]
    public async Task Nothing_is_owed_to_SAP_before_the_change_is_applied()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0012", locationId: Chennai);
        var transfer = await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);
        await DecideAsync(transfer.Value.Id, approved: true);

        (await SearchAsync()).Value.Rows.Single()
            .SapSyncStatus.ShouldBe(SapSyncStatus.NotRequired);
    }

    // ------------------------------------------------------------- negative

    [Fact]
    public async Task An_unknown_transfer_type_is_refused()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0013");

        var result = await RaiseAsync(asset, "Teleport", toEmployeeId: Bob);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Transfer.UnknownType");
    }

    [Fact]
    public async Task Each_type_needs_its_own_destination()
    {
        // CK_AssetTransferRequest_TypePair says this too, as a 500. This names
        // the field that is missing.
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0014");

        (await RaiseAsync(asset, TransferType.Branch)).Error!.Code
            .ShouldBe("Transfer.DestinationRequired");
        (await RaiseAsync(asset, TransferType.Employee)).Error!.Code
            .ShouldBe("Transfer.DestinationRequired");
        (await RaiseAsync(asset, TransferType.Department)).Error!.Code
            .ShouldBe("Transfer.DestinationRequired");
        (await RaiseAsync(asset, TransferType.CostCenter)).Error!.Code
            .ShouldBe("Transfer.DestinationRequired");
    }

    [Fact]
    public async Task A_transfer_for_an_unknown_asset_is_a_404()
    {
        await fixture.ResetAsync();

        (await RaiseAsync(987654, TransferType.Employee, toEmployeeId: Bob)).Error!.Code
            .ShouldBe("Asset.NotFound");
    }

    [Fact]
    public async Task An_asset_cannot_have_two_open_transfers()
    {
        // Two would apply in whatever order they were completed, and the second
        // would overwrite the first with values captured before it happened.
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0015", locationId: Chennai);
        await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);

        var second = await RaiseAsync(asset, TransferType.Employee, toEmployeeId: Bob);

        second.IsSuccess.ShouldBeFalse();
        second.Error!.Code.ShouldBe("Transfer.AlreadyOpen");
    }

    [Fact]
    public async Task A_transfer_cannot_be_decided_twice()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0016", locationId: Chennai);
        var transfer = await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);
        await DecideAsync(transfer.Value.Id, approved: true);

        (await DecideAsync(transfer.Value.Id, approved: false)).Error!.Code
            .ShouldBe("Transfer.AlreadyDecided");
    }

    [Fact]
    public async Task An_undecided_transfer_cannot_be_completed()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0017", locationId: Chennai);
        var transfer = await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);

        var result = await CompleteAsync(transfer.Value.Id);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Transfer.NotApproved");
    }

    [Fact]
    public async Task A_completed_transfer_cannot_be_completed_again()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0018", locationId: Chennai);
        var transfer = await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);
        await DecideAsync(transfer.Value.Id, approved: true);
        await CompleteAsync(transfer.Value.Id);

        (await CompleteAsync(transfer.Value.Id)).Error!.Code.ShouldBe("Transfer.NotApproved");
    }

    // ----------------------------------------------------------------- edge

    [Fact]
    public async Task A_completed_transfer_cannot_be_cancelled()
    {
        // The change is in the register and possibly already in SAP. Undoing it
        // is a NEW transfer the other way, which is the only true version.
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0019", locationId: Chennai);
        var transfer = await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);
        await DecideAsync(transfer.Value.Id, approved: true);
        await CompleteAsync(transfer.Value.Id);

        var result = await CancelAsync(transfer.Value.Id, "Changed our minds.");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Transfer.AlreadyCompleted");
    }

    [Fact]
    public async Task An_approved_transfer_can_still_be_cancelled()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0020", locationId: Chennai);
        var transfer = await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);
        await DecideAsync(transfer.Value.Id, approved: true);

        var result = await CancelAsync(transfer.Value.Id, "Branch reopened.");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(TransferStatus.Cancelled);
        (await fixture.CustodyOfAsync(asset)).Location.ShouldBe(Chennai, "nothing was applied");
    }

    [Fact]
    public async Task Cancelling_twice_is_harmless()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0021", locationId: Chennai);
        var transfer = await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);
        await CancelAsync(transfer.Value.Id, "Mistake.");

        (await CancelAsync(transfer.Value.Id, "Again.")).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task A_cancelled_transfer_frees_the_asset_for_another()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0022", locationId: Chennai);
        var first = await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);
        await CancelAsync(first.Value.Id, "Wrong branch.");

        (await RaiseAsync(asset, TransferType.Employee, toEmployeeId: Bob)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Completing_a_transfer_whose_asset_was_deleted_is_a_404()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0023", locationId: Chennai);
        var transfer = await RaiseAsync(asset, TransferType.Branch, toLocationId: Bangalore);
        await DecideAsync(transfer.Value.Id, approved: true);

        await using (var assets = fixture.NewAssetsContext())
        {
            var row = await assets.Assets.FindAsync(asset);
            row!.IsDeleted = true;
            await assets.SaveChangesAsync();
        }

        (await CompleteAsync(transfer.Value.Id)).Error!.Code.ShouldBe("Asset.NotFound");
    }

    [Fact]
    public async Task A_branch_administrator_sees_both_ends_and_the_placeless_ones()
    {
        await fixture.ResetAsync();
        var leaving = await fixture.AddAssetAsync("AST-0024", locationId: Chennai);
        var arriving = await fixture.AddAssetAsync("AST-0025", locationId: 9);
        var placeless = await fixture.AddAssetAsync("AST-0026", costCenter: "CC-100");
        await RaiseAsync(leaving, TransferType.Branch, toLocationId: Bangalore);
        await RaiseAsync(arriving, TransferType.Branch, toLocationId: Chennai);
        await RaiseAsync(placeless, TransferType.CostCenter, toCostCenter: "CC-200");

        fixture.CurrentUser.HasAllBranches = false;
        fixture.CurrentUser.BranchIds = new HashSet<int> { Chennai };

        (await SearchAsync()).Value.TotalCount.ShouldBe(3);
    }

    // -------------------------------------------------------------- helpers

    private Task<Result<RaiseTransferResponse>> RaiseAsync(
        int assetId,
        string transferType,
        int? toEmployeeId = null,
        int? toDepartmentId = null,
        int? toLocationId = null,
        string? toCostCenter = null)
    {
        var context = fixture.NewContext();
        var assets = fixture.NewAssetsContext();
        var handler = new RaiseTransferHandler(
            context, new AssetSnapshotReader(assets), fixture.Clock,
            fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new RaiseTransferCommand(
                assetId, transferType, toEmployeeId, toDepartmentId,
                toLocationId, toCostCenter, null),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<DecideTransferResponse>> DecideAsync(
        int id, bool approved, string? remarks = null)
    {
        var handler = new DecideTransferHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new DecideTransferCommand(id, approved, remarks),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<CompleteTransferResponse>> CompleteAsync(int id)
    {
        var context = fixture.NewContext();
        var assets = fixture.NewAssetsContext();
        var handler = new CompleteTransferHandler(
            context,
            new AssetCustody(assets, fixture.Clock, fixture.CurrentUser),
            new AssetTimeline(assets),
            fixture.Clock,
            fixture.CurrentUser,
            fixture.SqlErrors);
        return handler.HandleAsync(
            new CompleteTransferCommand(id, null), TestContext.Current.CancellationToken);
    }

    private Task<Result<CancelTransferResponse>> CancelAsync(int id, string reason)
    {
        var handler = new CancelTransferHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new CancelTransferCommand(id, reason), TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchTransferRequestsResponse>> SearchAsync()
    {
        var handler = new SearchTransferRequestsHandler(fixture.NewContext(), fixture.CurrentUser);
        return handler.HandleAsync(
            new SearchTransferRequestsQuery(null, null, null, null, 0, 50),
            TestContext.Current.CancellationToken);
    }
}
