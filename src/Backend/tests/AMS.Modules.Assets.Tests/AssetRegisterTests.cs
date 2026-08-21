using AMS.Modules.Assets.Features.CreateAssetClass;
using AMS.Modules.Assets.Features.CreateAssetStatus;
using AMS.Modules.Assets.Features.CreateAssetType;
using AMS.Modules.Assets.Features.DeleteAsset;
using AMS.Modules.Assets.Features.RegisterAsset;
using AMS.Modules.Assets.Features.SearchAssets;
using AMS.Modules.Assets.Features.UpdateAsset;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Tests;

/// <summary>
/// Catalogue screen: Asset Register. Features: Register an asset, Search filter
/// and page, Delete an asset, Bulk lines carry a quantity.
/// </summary>
/// <remarks>
/// Revision 3 made this the register for every asset the company owns, so the
/// interesting cases are no longer about laptops: a 495-strong line of
/// barricades and a software licence with no branch are both ordinary here.
/// </remarks>
[Collection(nameof(AssetsCollectionDefinition))]
public sealed class AssetRegisterTests(AssetsFixture fixture)
{
    // ------------------------------------------------------------- positive

    [Fact]
    public async Task An_asset_can_be_registered_and_found()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();

        var created = await RegisterAsync("AST-0001", "A laptop", type, status);

        created.IsSuccess.ShouldBeTrue();
        var page = (await SearchAsync()).Value;
        page.TotalCount.ShouldBe(1);
        page.Rows.Single().AssetNumber.ShouldBe("AST-0001");
        page.Rows.Single().Quantity.ShouldBe(1m);
    }

    [Fact]
    public async Task Registering_writes_the_first_line_of_the_timeline()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();

        var created = await RegisterAsync("AST-0002", "A laptop", type, status);

        await using var context = fixture.NewAssetsContext();
        var events = await context.AssetEvents
            .Where(e => e.AssetId == created.Value.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Single().EventType.ShouldBe("Registered");
    }

    [Fact]
    public async Task A_bulk_line_carries_a_quantity_and_a_unit()
    {
        await fixture.ResetAsync();
        var (_, status, _) = await SeedLookupsAsync();
        var barricades = await CreateTypeAsync("Barricades", isAllocatable: false, isBulkDefault: true);

        var created = await RegisterAsync(
            "PLM-0001", "Barricades", barricades, status,
            isBulk: true, quantity: 495m, unitOfMeasure: "Nos");

        created.IsSuccess.ShouldBeTrue();
        var row = (await SearchAsync()).Value.Rows.Single();
        row.IsBulk.ShouldBeTrue();
        row.Quantity.ShouldBe(495m);
        row.UnitOfMeasure.ShouldBe("Nos");
    }

    [Fact]
    public async Task An_asset_can_be_classified_for_the_accounts()
    {
        await fixture.ResetAsync();
        var (type, status, assetClass) = await SeedLookupsAsync();

        await RegisterAsync("AST-0003", "A desk", type, status, assetClassId: assetClass);

        (await SearchAsync()).Value.Rows.Single().ClassName.ShouldBe("Furniture & Fixtures");
    }

    [Fact]
    public async Task An_unclassified_asset_is_normal_and_lists_without_a_class()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();

        await RegisterAsync("AST-0004", "Keyed before the finance import", type, status);

        (await SearchAsync()).Value.Rows.Single().ClassName.ShouldBeNull();
    }

    [Fact]
    public async Task An_asset_can_be_edited()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();
        var created = await RegisterAsync("AST-0005", "Old name", type, status);

        var updated = await UpdateAsync(
            created.Value.Id, "AST-0005", "New name", type, status, make: "Dell", model: "Latitude");

        updated.IsSuccess.ShouldBeTrue();
        var row = (await SearchAsync()).Value.Rows.Single();
        row.AssetName.ShouldBe("New name");
        row.Make.ShouldBe("Dell");
        row.Model.ShouldBe("Latitude");
    }

    [Fact]
    public async Task Changing_the_status_writes_a_timeline_line_and_an_ordinary_edit_does_not()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();
        var other = (await CreateStatusAsync("Under Repair", 4)).Value.Id;
        var created = await RegisterAsync("AST-0006", "A laptop", type, status);

        await UpdateAsync(created.Value.Id, "AST-0006", "Renamed only", type, status);
        (await EventTypesAsync(created.Value.Id)).ShouldBe(["Registered"]);

        await UpdateAsync(created.Value.Id, "AST-0006", "Renamed only", type, other);
        (await EventTypesAsync(created.Value.Id)).ShouldBe(["Registered", "StatusChanged"]);
    }

    [Fact]
    public async Task Deleting_is_soft_and_leaves_the_timeline_behind()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();
        var created = await RegisterAsync("AST-0007", "A laptop", type, status);

        var deleted = await DeleteAsync(created.Value.Id, "Written off after a spill");

        deleted.IsSuccess.ShouldBeTrue();
        (await SearchAsync()).Value.TotalCount.ShouldBe(0);
        (await SearchAsync(includeDeleted: true)).Value.Rows.Single().IsDeleted.ShouldBeTrue();
        (await EventTypesAsync(created.Value.Id)).ShouldBe(["Registered", "Deleted"]);
    }

    // -------------------------------------------------------- search and page

    [Fact]
    public async Task The_grid_pages_and_reports_the_full_total()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();
        for (var i = 1; i <= 5; i++)
        {
            await RegisterAsync($"AST-{i:0000}", $"Asset {i}", type, status);
        }

        var page = (await SearchAsync(skip: 2, take: 2)).Value;

        page.TotalCount.ShouldBe(5);
        page.Rows.Select(r => r.AssetNumber).ShouldBe(["AST-0003", "AST-0004"]);
    }

    [Fact]
    public async Task Search_matches_number_name_serial_make_and_model()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();
        await RegisterAsync("AST-0001", "A laptop", type, status,
            serialNumber: "SN-XYZ", make: "Dell", model: "Latitude");
        await RegisterAsync("AST-0002", "A chair", type, status);

        (await SearchAsync(search: "XYZ")).Value.TotalCount.ShouldBe(1);
        (await SearchAsync(search: "Dell")).Value.TotalCount.ShouldBe(1);
        (await SearchAsync(search: "Latitude")).Value.TotalCount.ShouldBe(1);
        (await SearchAsync(search: "chair")).Value.TotalCount.ShouldBe(1);
        (await SearchAsync(search: "AST-000")).Value.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task A_branch_administrator_sees_their_own_branches_and_the_unplaced_stock()
    {
        // Bulk lines have no single location and an asset in transit belongs to
        // neither end. Hiding those would make stock invisible to the only
        // people who could act on it.
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();
        var bulkType = await CreateTypeAsync("Barricades", isAllocatable: false, isBulkDefault: true);
        await RegisterAsync("AST-MINE", "At my branch", type, status, locationId: 1);
        await RegisterAsync("AST-THEIRS", "At another branch", type, status, locationId: 2);
        await RegisterAsync("AST-BULK", "Unplaced stock", bulkType, status,
            isBulk: true, quantity: 10m, unitOfMeasure: "Nos");

        fixture.CurrentUser.HasAllBranches = false;
        fixture.CurrentUser.BranchIds = new HashSet<int> { 1 };
        try
        {
            var rows = (await SearchAsync()).Value.Rows.Select(r => r.AssetNumber).ToArray();
            rows.ShouldBe(["AST-BULK", "AST-MINE"], ignoreOrder: true);
        }
        finally
        {
            fixture.CurrentUser.HasAllBranches = true;
            fixture.CurrentUser.BranchIds = new HashSet<int>();
        }
    }

    [Fact]
    public async Task The_filters_select_by_type_class_status_and_bulk()
    {
        await fixture.ResetAsync();
        var (type, status, assetClass) = await SeedLookupsAsync();
        var bulkType = await CreateTypeAsync("Barricades", isAllocatable: false, isBulkDefault: true);
        var otherStatus = (await CreateStatusAsync("Under Repair", 4)).Value.Id;
        await RegisterAsync("AST-0001", "A laptop", type, status, assetClassId: assetClass);
        await RegisterAsync("AST-0002", "A broken laptop", type, otherStatus);
        await RegisterAsync("AST-0003", "Barricades", bulkType, status,
            isBulk: true, quantity: 20m, unitOfMeasure: "Nos");

        (await SearchAsync(assetTypeId: bulkType)).Value.TotalCount.ShouldBe(1);
        (await SearchAsync(assetClassId: assetClass)).Value.TotalCount.ShouldBe(1);
        (await SearchAsync(assetStatusId: otherStatus)).Value.TotalCount.ShouldBe(1);
        (await SearchAsync(isBulk: true)).Value.TotalCount.ShouldBe(1);
        (await SearchAsync(isBulk: false)).Value.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task An_empty_register_is_an_empty_page_and_not_a_failure()
    {
        await fixture.ResetAsync();

        var result = await SearchAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Rows.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    // ------------------------------------------------------------- negative

    [Fact]
    public async Task Two_assets_cannot_share_a_number()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();
        await RegisterAsync("AST-0001", "First", type, status);

        var result = await RegisterAsync("AST-0001", "Second", type, status);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Asset.NumberTaken");
    }

    [Fact]
    public async Task An_asset_cannot_be_registered_against_lookups_that_do_not_exist()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();

        (await RegisterAsync("A", "x", 987654, status)).Error!.Code.ShouldBe("AssetType.NotFound");
        (await RegisterAsync("B", "x", type, 987654)).Error!.Code.ShouldBe("AssetStatus.NotFound");
        (await RegisterAsync("C", "x", type, status, assetClassId: 987654)).Error!.Code
            .ShouldBe("AssetClass.NotFound");
    }

    [Fact]
    public async Task An_unknown_asset_cannot_be_edited_or_deleted()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();

        (await UpdateAsync(987654, "X", "Ghost", type, status)).Error!.Code.ShouldBe("Asset.NotFound");
        (await DeleteAsync(987654)).Error!.Code.ShouldBe("Asset.NotFound");
    }

    [Fact]
    public async Task A_deleted_asset_cannot_be_edited()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();
        var created = await RegisterAsync("AST-0008", "A laptop", type, status);
        await DeleteAsync(created.Value.Id);

        var result = await UpdateAsync(created.Value.Id, "AST-0008", "Back again", type, status);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Asset.Deleted");
    }

    // ----------------------------------------------------------------- edge

    [Fact]
    public async Task A_unit_asset_cannot_carry_a_quantity()
    {
        // CK_Asset_UnitQuantityIsOne is what makes "every allocatable asset has
        // Quantity = 1" a database proof. This is the message that says so
        // before the database has to.
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();

        var result = await RegisterAsync("AST-0009", "A laptop", type, status, quantity: 5m);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Asset.UnitQuantityIsOne");
    }

    [Fact]
    public async Task A_bulk_line_without_a_unit_is_refused()
    {
        await fixture.ResetAsync();
        var (_, status, _) = await SeedLookupsAsync();
        var bulkType = await CreateTypeAsync("Barricades", isAllocatable: false, isBulkDefault: true);

        var result = await RegisterAsync(
            "PLM-0002", "Barricades", bulkType, status, isBulk: true, quantity: 12m);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Asset.BulkNeedsUnit");
    }

    [Fact]
    public async Task A_bulk_line_cannot_be_held_at_one_branch()
    {
        await fixture.ResetAsync();
        var (_, status, _) = await SeedLookupsAsync();
        var bulkType = await CreateTypeAsync("Barricades", isAllocatable: false, isBulkDefault: true);

        var result = await RegisterAsync(
            "PLM-0003", "Barricades", bulkType, status,
            isBulk: true, quantity: 12m, unitOfMeasure: "Nos", locationId: 1);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Asset.BulkNotHeld");
    }

    [Fact]
    public async Task An_allocatable_type_cannot_be_recorded_in_bulk()
    {
        // Almost always somebody ticking the wrong box on a laptop, and the
        // result would be a laptop nobody can issue.
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();

        var result = await RegisterAsync(
            "AST-0010", "Laptops", type, status, isBulk: true, quantity: 8m, unitOfMeasure: "Nos");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Asset.TypeIsNotBulk");
    }

    [Fact]
    public async Task A_licence_cannot_be_given_a_branch()
    {
        await fixture.ResetAsync();
        var (_, status, _) = await SeedLookupsAsync();
        var licences = await CreateTypeAsync("Licences", isPhysical: false);

        var result = await RegisterAsync("SW-0001", "Office 365", licences, status, locationId: 1);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Asset.NotPhysical");
    }

    [Fact]
    public async Task Quantity_defaults_to_one_when_the_form_leaves_it_out()
    {
        // The column defaults to 1 and so does the entity. This proves the
        // command path agrees, which is what CK_Asset_UnitQuantityIsOne needs.
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();

        var created = await RegisterAsync("AST-0011", "A laptop", type, status);

        created.IsSuccess.ShouldBeTrue();
        (await SearchAsync()).Value.Rows.Single().Quantity.ShouldBe(1m);
    }

    [Fact]
    public async Task Deleting_twice_is_harmless_and_writes_one_timeline_line()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();
        var created = await RegisterAsync("AST-0012", "A laptop", type, status);

        await DeleteAsync(created.Value.Id);
        var second = await DeleteAsync(created.Value.Id);

        second.IsSuccess.ShouldBeTrue();
        (await EventTypesAsync(created.Value.Id)).ShouldBe(["Registered", "Deleted"]);
    }

    [Fact]
    public async Task A_bulk_line_with_stock_on_hand_cannot_be_deleted()
    {
        await fixture.ResetAsync();
        var (_, status, _) = await SeedLookupsAsync();
        var bulkType = await CreateTypeAsync("Barricades", isAllocatable: false, isBulkDefault: true);
        var created = await RegisterAsync(
            "PLM-0004", "Barricades", bulkType, status,
            isBulk: true, quantity: 20m, unitOfMeasure: "Nos");
        await AddHoldingAsync(created.Value.Id, locationId: 1, quantity: 20m);

        var result = await DeleteAsync(created.Value.Id);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Asset.StillInStock");
    }

    [Fact]
    public async Task A_bulk_line_whose_stock_has_gone_can_be_deleted()
    {
        // The guard is about stock on hand, not about ever having had any.
        await fixture.ResetAsync();
        var (_, status, _) = await SeedLookupsAsync();
        var bulkType = await CreateTypeAsync("Barricades", isAllocatable: false, isBulkDefault: true);
        var created = await RegisterAsync(
            "PLM-0005", "Barricades", bulkType, status,
            isBulk: true, quantity: 20m, unitOfMeasure: "Nos");
        await AddHoldingAsync(created.Value.Id, locationId: 1, quantity: 0m);

        (await DeleteAsync(created.Value.Id)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Text_is_trimmed_and_blanks_become_null()
    {
        await fixture.ResetAsync();
        var (type, status, _) = await SeedLookupsAsync();

        await RegisterAsync("  AST-0013  ", "  A laptop  ", type, status,
            serialNumber: "   ", make: "  Dell  ");

        var row = (await SearchAsync()).Value.Rows.Single();
        row.AssetNumber.ShouldBe("AST-0013");
        row.AssetName.ShouldBe("A laptop");
        row.SerialNumber.ShouldBeNull();
        row.Make.ShouldBe("Dell");
    }

    // -------------------------------------------------------------- helpers

    private async Task<(int Type, int Status, int Class)> SeedLookupsAsync()
    {
        var type = await CreateTypeAsync("Laptops");
        var status = (await CreateStatusAsync("In Stock", 1)).Value.Id;
        var assetClass = (await CreateClassAsync()).Value.Id;
        return (type, status, assetClass);
    }

    private async Task<int> CreateTypeAsync(
        string name, bool isAllocatable = true, bool isPhysical = true, bool isBulkDefault = false)
    {
        var handler = new CreateAssetTypeHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        var result = await handler.HandleAsync(
            new CreateAssetTypeCommand(
                name, null, isAllocatable, isPhysical, isBulkDefault, false, false, false, false),
            TestContext.Current.CancellationToken);
        return result.Value.Id;
    }

    private Task<Result<CreateAssetStatusResponse>> CreateStatusAsync(string name, int order)
    {
        var handler = new CreateAssetStatusHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new CreateAssetStatusCommand(name, false, order), TestContext.Current.CancellationToken);
    }

    private Task<Result<CreateAssetClassResponse>> CreateClassAsync()
    {
        var handler = new CreateAssetClassHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new CreateAssetClassCommand("F & F", "Furniture & Fixtures", "Furniture & Fixtures", true, false),
            TestContext.Current.CancellationToken);
    }

    private async Task AddHoldingAsync(int assetId, int locationId, decimal quantity)
    {
        await using var context = fixture.NewAssetsContext();
        context.AssetHoldings.Add(new Modules.Assets.Domain.AssetHolding
        {
            AssetId = assetId,
            LocationId = locationId,
            OnHandQuantity = quantity,
            CreatedOnUtc = fixture.Clock.UtcNow,
            CreatedBy = "test",
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<string[]> EventTypesAsync(int assetId)
    {
        await using var context = fixture.NewAssetsContext();
        return await context.AssetEvents
            .Where(e => e.AssetId == assetId)
            .OrderBy(e => e.Id)
            .Select(e => e.EventType)
            .ToArrayAsync(TestContext.Current.CancellationToken);
    }

    private Task<Result<RegisterAssetResponse>> RegisterAsync(
        string number,
        string name,
        int typeId,
        int statusId,
        int? assetClassId = null,
        string? serialNumber = null,
        string? make = null,
        string? model = null,
        int? locationId = null,
        bool isBulk = false,
        decimal quantity = 1m,
        string? unitOfMeasure = null)
    {
        var context = fixture.NewAssetsContext();
        var handler = new RegisterAssetHandler(
            context, new AssetTimeline(context), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new RegisterAssetCommand(
                number.Trim(), name.Trim(),
                string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim(),
                typeId, assetClassId,
                string.IsNullOrWhiteSpace(make) ? null : make.Trim(),
                string.IsNullOrWhiteSpace(model) ? null : model.Trim(),
                statusId, locationId, null, null, null,
                isBulk, quantity, unitOfMeasure, null),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<UpdateAssetResponse>> UpdateAsync(
        int id,
        string number,
        string name,
        int typeId,
        int statusId,
        string? make = null,
        string? model = null,
        int? locationId = null,
        bool isBulk = false,
        decimal quantity = 1m,
        string? unitOfMeasure = null)
    {
        var context = fixture.NewAssetsContext();
        var handler = new UpdateAssetHandler(
            context, new AssetTimeline(context), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new UpdateAssetCommand(
                id, number, name, null, typeId, null, make, model, statusId,
                locationId, null, null, null, isBulk, quantity, unitOfMeasure, null),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<DeleteAssetResponse>> DeleteAsync(int id, string? reason = null)
    {
        var context = fixture.NewAssetsContext();
        var handler = new DeleteAssetHandler(
            context, new AssetTimeline(context), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new DeleteAssetCommand(id, reason), TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchAssetsResponse>> SearchAsync(
        string? search = null,
        int? assetTypeId = null,
        int? assetClassId = null,
        int? assetStatusId = null,
        int? locationId = null,
        int? employeeId = null,
        int? departmentId = null,
        string? costCenter = null,
        string? sapAssetNumber = null,
        string? sapPlant = null,
        DateOnly? acquiredFrom = null,
        DateOnly? acquiredTo = null,
        bool? isBulk = null,
        bool includeDeleted = false,
        int skip = 0,
        int take = 50)
    {
        var handler = new SearchAssetsHandler(fixture.NewAssetsContext(), fixture.CurrentUser);
        return handler.HandleAsync(
            new SearchAssetsQuery(
                search, assetTypeId, assetClassId, assetStatusId, locationId, employeeId,
                departmentId, costCenter, sapAssetNumber, sapPlant, acquiredFrom, acquiredTo,
                isBulk, null, includeDeleted, skip, take),
            TestContext.Current.CancellationToken);
    }
}
