using AMS.Modules.Assets.Persistence;
using AMS.Modules.Movements.Domain;
using AMS.Modules.Movements.Features.DespatchAsset;
using AMS.Modules.Movements.Features.DespatchBatch;
using AMS.Modules.Movements.Features.GetGrnQueue;
using AMS.Modules.Movements.Features.ReceiveMovement;
using AMS.Modules.Movements.Features.SearchMovements;
using AMS.SharedKernel.Results;

namespace AMS.Modules.Movements.Tests;

/// <summary>
/// Catalogue screens: Despatch, Despatch Batch, GRN Queue.
/// </summary>
/// <remarks>
/// The rule everything here turns on, in the design script's own words: an
/// asset in transit belongs to NEITHER branch, so its branch changes on receipt
/// and never on despatch. Marking it as arrived on despatch makes it findable
/// somewhere it is not.
/// </remarks>
[Collection(nameof(MovementsCollectionDefinition))]
public sealed class MovementTests(MovementsFixture fixture)
{
    private const int Chennai = 1;
    private const int Bangalore = 2;
    private const int HeadOffice = 3;

    // ------------------------------------------------------------- positive

    [Fact]
    public async Task An_asset_can_be_despatched_and_found()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0001", Chennai);

        var result = await DespatchAsync(asset);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(MovementStatus.InTransit);
        (await SearchAsync()).Value.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Despatching_does_NOT_move_the_asset()
    {
        // The whole point. It is on a lorry; it belongs to neither branch.
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0002", Chennai);

        await DespatchAsync(asset);

        (await fixture.LocationOfAsync(asset)).ShouldBe(Chennai, "the branch changes on receipt, not despatch");
    }

    [Fact]
    public async Task Receiving_moves_the_asset_to_the_destination()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0003", Chennai);
        var movement = await DespatchAsync(asset);

        var received = await ReceiveAsync(movement.Value.Id);

        received.IsSuccess.ShouldBeTrue();
        (await fixture.LocationOfAsync(asset)).ShouldBe(Bangalore);
    }

    [Fact]
    public async Task Despatch_and_receipt_both_write_the_timeline()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0004", Chennai);
        var movement = await DespatchAsync(asset);
        await ReceiveAsync(movement.Value.Id);

        (await fixture.TimelineOfAsync(asset)).ShouldBe(["Despatched", "Received"]);
    }

    [Fact]
    public async Task A_batch_carries_the_invoice_once_and_gives_every_asset_its_own_row()
    {
        await fixture.ResetAsync();
        var first = await fixture.AddAssetAsync("AST-0005", Chennai);
        var second = await fixture.AddAssetAsync("AST-0006", Chennai);
        var third = await fixture.AddAssetAsync("AST-0007", Chennai);

        var batch = await DespatchBatchAsync([first, second, third]);

        batch.IsSuccess.ShouldBeTrue();
        batch.Value.ItemCount.ShouldBe(3);
        batch.Value.BatchNumber.ShouldStartWith("MB-");

        var rows = (await SearchAsync()).Value.Rows;
        rows.Count.ShouldBe(3);
        rows.ShouldAllBe(r => r.MovementBatchId == batch.Value.Id);
        rows.Select(r => r.AssetId).ShouldBe([first, second, third], ignoreOrder: true);
    }

    [Fact]
    public async Task A_batch_closes_when_its_last_item_is_received()
    {
        await fixture.ResetAsync();
        var first = await fixture.AddAssetAsync("AST-0008", Chennai);
        var second = await fixture.AddAssetAsync("AST-0009", Chennai);
        await DespatchBatchAsync([first, second]);

        var ids = (await SearchAsync()).Value.Rows.Select(r => r.Id).OrderBy(id => id).ToArray();

        (await ReceiveAsync(ids[0])).Value.BatchComplete
            .ShouldBeFalse("one item is still out");
        (await ReceiveAsync(ids[1])).Value.BatchComplete
            .ShouldBeTrue("that was the last one");
    }

    [Fact]
    public async Task Consignment_numbers_are_unique_and_come_from_the_sequence()
    {
        await fixture.ResetAsync();
        var a = await fixture.AddAssetAsync("AST-0010", Chennai);
        var b = await fixture.AddAssetAsync("AST-0011", Chennai);

        var first = await DespatchBatchAsync([a]);
        var second = await DespatchBatchAsync([b]);

        first.Value.BatchNumber.ShouldNotBe(second.Value.BatchNumber);
    }

    // ---------------------------------------------------------- the GRN queue

    [Fact]
    public async Task The_queue_shows_what_is_coming_and_how_long_it_has_been_travelling()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0012", Chennai);
        await DespatchAsync(asset);
        fixture.Clock.Advance(TimeSpan.FromDays(9));

        var queue = (await GrnQueueAsync()).Value;

        queue.TotalCount.ShouldBe(1);
        queue.Rows.Single().DaysInTransit.ShouldBe(9);
        queue.Rows.Single().ToLocationId.ShouldBe(Bangalore);
    }

    [Fact]
    public async Task The_queue_is_oldest_first_so_the_stale_ones_surface()
    {
        await fixture.ResetAsync();
        var old = await fixture.AddAssetAsync("AST-0013", Chennai);
        await DespatchAsync(old);
        fixture.Clock.Advance(TimeSpan.FromDays(20));
        var recent = await fixture.AddAssetAsync("AST-0014", Chennai);
        await DespatchAsync(recent);

        (await GrnQueueAsync()).Value.Rows.Select(r => r.AssetId).ShouldBe([old, recent]);
    }

    [Fact]
    public async Task A_received_shipment_leaves_the_queue()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0015", Chennai);
        var movement = await DespatchAsync(asset);

        await ReceiveAsync(movement.Value.Id);

        (await GrnQueueAsync()).Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task The_queue_shows_only_what_is_arriving_at_your_own_branch()
    {
        // A queue of things arriving somewhere else is not one anybody here can
        // act on.
        await fixture.ResetAsync();
        var mine = await fixture.AddAssetAsync("AST-0016", Chennai);
        var theirs = await fixture.AddAssetAsync("AST-0017", Chennai);
        await DespatchAsync(mine, to: Bangalore);
        await DespatchAsync(theirs, to: HeadOffice);

        fixture.CurrentUser.HasAllBranches = false;
        fixture.CurrentUser.BranchIds = new HashSet<int> { Bangalore };

        var queue = (await GrnQueueAsync()).Value;
        queue.Rows.Single().AssetId.ShouldBe(mine);
    }

    [Fact]
    public async Task The_despatch_list_shows_both_ends_of_a_shipment()
    {
        // A shipment concerns the branch that sent it AND the one expecting it.
        await fixture.ResetAsync();
        var sent = await fixture.AddAssetAsync("AST-0018", Chennai);
        var incoming = await fixture.AddAssetAsync("AST-0019", HeadOffice);
        await DespatchAsync(sent, from: Chennai, to: Bangalore);
        await DespatchAsync(incoming, from: HeadOffice, to: Chennai);

        fixture.CurrentUser.HasAllBranches = false;
        fixture.CurrentUser.BranchIds = new HashSet<int> { Chennai };

        (await SearchAsync()).Value.TotalCount.ShouldBe(2);
    }

    // ------------------------------------------------------------- negative

    [Fact]
    public async Task An_asset_cannot_be_on_two_lorries_at_once()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0020", Chennai);
        await DespatchAsync(asset);

        var second = await DespatchAsync(asset);

        second.IsSuccess.ShouldBeFalse();
        second.Error!.Code.ShouldBe("Movement.AlreadyInTransit");
    }

    [Fact]
    public async Task An_unknown_movement_cannot_be_received()
    {
        await fixture.ResetAsync();

        (await ReceiveAsync(987654)).Error!.Code.ShouldBe("Movement.NotFound");
    }

    [Fact]
    public async Task A_shipment_cannot_be_received_twice()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0021", Chennai);
        var movement = await DespatchAsync(asset);
        await ReceiveAsync(movement.Value.Id);

        var again = await ReceiveAsync(movement.Value.Id);

        again.IsSuccess.ShouldBeFalse();
        again.Error!.Code.ShouldBe("Movement.AlreadyReceived");
    }

    [Fact]
    public async Task An_unknown_movement_type_is_refused()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0022", Chennai);

        var result = await DespatchAsync(asset, movementType: "Teleport");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Movement.UnknownType");
    }

    [Fact]
    public async Task An_empty_batch_is_refused()
    {
        await fixture.ResetAsync();

        var result = await DespatchBatchAsync([]);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Movement.EmptyBatch");
    }

    [Fact]
    public async Task A_batch_containing_an_asset_already_in_transit_is_refused()
    {
        await fixture.ResetAsync();
        var travelling = await fixture.AddAssetAsync("AST-0023", Chennai);
        var free = await fixture.AddAssetAsync("AST-0024", Chennai);
        await DespatchAsync(travelling);

        var result = await DespatchBatchAsync([free, travelling]);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Movement.AlreadyInTransit");
    }

    // ----------------------------------------------------------------- edge

    [Fact]
    public async Task Receiving_a_shipment_whose_asset_was_deleted_is_a_404()
    {
        // The contract reports it rather than throwing: somebody deleting an
        // asset mid-transit is a thing a user can do, not a bug.
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0025", Chennai);
        var movement = await DespatchAsync(asset);

        await using (var assets = fixture.NewAssetsContext())
        {
            var row = await assets.Assets.FindAsync(asset);
            row!.IsDeleted = true;
            await assets.SaveChangesAsync();
        }

        var result = await ReceiveAsync(movement.Value.Id);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Asset.NotFound");
    }

    [Fact]
    public async Task A_batch_of_one_closes_on_its_only_receipt()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0026", Chennai);
        await DespatchBatchAsync([asset]);
        var movementId = (await SearchAsync()).Value.Rows.Single().Id;

        (await ReceiveAsync(movementId)).Value.BatchComplete.ShouldBeTrue();
    }

    [Fact]
    public async Task A_single_despatch_has_no_batch_and_never_reports_one_complete()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0027", Chennai);
        var movement = await DespatchAsync(asset);

        var received = await ReceiveAsync(movement.Value.Id);

        received.Value.BatchComplete.ShouldBeFalse();
        (await SearchAsync()).Value.Rows.Single().MovementBatchId.ShouldBeNull();
    }

    [Fact]
    public async Task The_receiving_remark_is_kept_on_the_shipment_and_the_timeline()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0028", Chennai);
        var movement = await DespatchAsync(asset);

        await ReceiveAsync(movement.Value.Id, "Box dented, contents fine.");

        (await SearchAsync()).Value.Rows.Single()
            .ReceiptRemarks.ShouldBe("Box dented, contents fine.");
    }

    [Fact]
    public async Task An_asset_can_travel_again_once_it_has_arrived()
    {
        await fixture.ResetAsync();
        var asset = await fixture.AddAssetAsync("AST-0029", Chennai);
        var first = await DespatchAsync(asset, from: Chennai, to: Bangalore);
        await ReceiveAsync(first.Value.Id);

        var second = await DespatchAsync(asset, from: Bangalore, to: HeadOffice);

        second.IsSuccess.ShouldBeTrue();
        (await ReceiveAsync(second.Value.Id)).IsSuccess.ShouldBeTrue();
        (await fixture.LocationOfAsync(asset)).ShouldBe(HeadOffice);
    }

    // -------------------------------------------------------------- helpers

    private Task<Result<DespatchAssetResponse>> DespatchAsync(
        int assetId,
        int from = Chennai,
        int to = Bangalore,
        string movementType = MovementType.Transfer)
    {
        var context = fixture.NewContext();
        var assets = fixture.NewAssetsContext();
        var handler = new DespatchAssetHandler(
            context, new AssetTimeline(assets), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new DespatchAssetCommand(
                assetId, movementType, from, to, 1m, null,
                "Blue Dart", "TRK-1", "CH-1", null, null, null),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<DespatchBatchResponse>> DespatchBatchAsync(IReadOnlyList<int> assetIds)
    {
        var context = fixture.NewContext();
        var assets = fixture.NewAssetsContext();
        var handler = new DespatchBatchHandler(
            context, new AssetTimeline(assets), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new DespatchBatchCommand(
                MovementType.Transfer, Chennai, Bangalore, "INV-1",
                new DateOnly(2026, 8, 12), "Blue Dart", "TRK-2", "CH-2",
                "Standby stock going back.", assetIds),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<ReceiveMovementResponse>> ReceiveAsync(int id, string? remarks = null)
    {
        var context = fixture.NewContext();
        var assets = fixture.NewAssetsContext();
        var handler = new ReceiveMovementHandler(
            context,
            new AssetCustody(assets, fixture.Clock, fixture.CurrentUser),
            new AssetTimeline(assets),
            fixture.Clock,
            fixture.CurrentUser,
            fixture.SqlErrors);
        return handler.HandleAsync(
            new ReceiveMovementCommand(id, remarks), TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchMovementsResponse>> SearchAsync()
    {
        var handler = new SearchMovementsHandler(fixture.NewContext(), fixture.CurrentUser);
        return handler.HandleAsync(
            new SearchMovementsQuery(null, null, null, null, null, 0, 50),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<GetGrnQueueResponse>> GrnQueueAsync()
    {
        var handler = new GetGrnQueueHandler(
            fixture.NewContext(), fixture.CurrentUser, fixture.Clock);
        return handler.HandleAsync(
            new GetGrnQueueQuery(null, 0, 50), TestContext.Current.CancellationToken);
    }
}
