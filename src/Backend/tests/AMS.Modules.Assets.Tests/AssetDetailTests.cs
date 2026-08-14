using AMS.Modules.Assets.Features.CreateAssetStatus;
using AMS.Modules.Assets.Features.CreateAssetType;
using AMS.Modules.Assets.Features.DefineCustomField;
using AMS.Modules.Assets.Features.GetAsset;
using AMS.Modules.Assets.Features.GetAssetTimeline;
using AMS.Modules.Assets.Features.RegisterAsset;
using AMS.Modules.Assets.Features.SaveAssetDetails;
using AMS.Modules.Assets.Features.SetAssetCustomValues;
using AMS.Modules.Assets.Features.UpdateAsset;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Results;

namespace AMS.Modules.Assets.Tests;

/// <summary>
/// Catalogue screen: Asset Detail and Timeline. Features: Asset detail and
/// timeline, Hardware details, Vehicle details, Calibration and instrument
/// details, Purchase and warranty, Fill custom fields, Book values mirrored
/// from SAP.
/// </summary>
[Collection(nameof(AssetsCollectionDefinition))]
public sealed class AssetDetailTests(AssetsFixture fixture)
{
    // ------------------------------------------------------- detail: positive

    [Fact]
    public async Task The_detail_screen_gets_the_record_and_its_lookups_in_one_call()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var asset = await RegisterAsync("AST-0001", "A laptop", type, status);

        var result = await GetAsync(asset);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Asset.AssetNumber.ShouldBe("AST-0001");
        result.Value.Asset.TypeName.ShouldBe("Laptops");
        result.Value.Asset.StatusName.ShouldBe("In Stock");
    }

    [Fact]
    public async Task The_type_flags_tell_the_screen_which_sections_to_draw()
    {
        await fixture.ResetAsync();
        var (_, status) = await SeedAsync();
        var vehicles = await CreateTypeAsync("Vehicles", tracksVehicle: true);
        var asset = await RegisterAsync("VEH-0001", "A van", vehicles, status);

        var core = (await GetAsync(asset)).Value.Asset;

        core.TracksVehicle.ShouldBeTrue();
        core.TracksHardware.ShouldBeFalse();
        core.TracksSoftware.ShouldBeFalse();
        core.TracksCalibration.ShouldBeFalse();
    }

    [Fact]
    public async Task Hardware_details_can_be_saved_and_read_back()
    {
        await fixture.ResetAsync();
        var (_, status) = await SeedAsync();
        var laptops = await CreateTypeAsync("Laptops HW", tracksHardware: true);
        var asset = await RegisterAsync("AST-0002", "A laptop", laptops, status);

        var saved = await SaveDetailsAsync(asset, hardware: new SaveAssetDetailsCommand.HardwareInput(
            "HO-LAP-01", "Laptop", "i7", 16, 512, null, null, "00:11:22:33:44:55", "10.0.0.5"));

        saved.IsSuccess.ShouldBeTrue();
        saved.Value.Saved.ShouldBe(["Hardware"]);
        var detail = (await GetAsync(asset)).Value.HardwareDetail.ShouldNotBeNull();
        detail.Hostname.ShouldBe("HO-LAP-01");
        detail.MemoryGb.ShouldBe(16);
    }

    [Fact]
    public async Task Saving_details_twice_updates_rather_than_duplicates()
    {
        await fixture.ResetAsync();
        var (_, status) = await SeedAsync();
        var laptops = await CreateTypeAsync("Laptops HW", tracksHardware: true);
        var asset = await RegisterAsync("AST-0003", "A laptop", laptops, status);

        await SaveDetailsAsync(asset, hardware: new SaveAssetDetailsCommand.HardwareInput(
            "FIRST", null, null, 8, null, null, null, null, null));
        await SaveDetailsAsync(asset, hardware: new SaveAssetDetailsCommand.HardwareInput(
            "SECOND", null, null, 32, null, null, null, null, null));

        var detail = (await GetAsync(asset)).Value.HardwareDetail.ShouldNotBeNull();
        detail.Hostname.ShouldBe("SECOND");
        detail.MemoryGb.ShouldBe(32);
    }

    [Fact]
    public async Task Vehicle_and_instrument_details_are_kept_on_their_own_records()
    {
        await fixture.ResetAsync();
        var (_, status) = await SeedAsync();
        var vehicles = await CreateTypeAsync("Vehicles", tracksVehicle: true);
        var meters = await CreateTypeAsync("Test Meters", tracksCalibration: true);
        var van = await RegisterAsync("VEH-0002", "A van", vehicles, status);
        var meter = await RegisterAsync("INS-0001", "A test meter", meters, status);

        await SaveDetailsAsync(van, vehicle: new SaveAssetDetailsCommand.VehicleInput(
            "TN-01-AB-1234", "CH1", "EN1", "Diesel",
            new DateOnly(2027, 3, 31), new DateOnly(2026, 12, 31), new DateOnly(2027, 1, 31), 42_000));
        await SaveDetailsAsync(meter, instrument: new SaveAssetDetailsCommand.InstrumentInput(
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), 12, "NABL Lab", "CERT-9", "0-100V", "0.5"));

        (await GetAsync(van)).Value.VehicleDetail.ShouldNotBeNull()
            .RegistrationNumber.ShouldBe("TN-01-AB-1234");
        (await GetAsync(meter)).Value.InstrumentDetail.ShouldNotBeNull()
            .CalibrationEndDate.ShouldBe(new DateOnly(2027, 1, 1));
    }

    [Fact]
    public async Task Purchase_and_warranty_apply_to_anything()
    {
        // Not gated on a flag: everything on the register was bought.
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var asset = await RegisterAsync("AST-0004", "A chair", type, status);

        var saved = await SaveDetailsAsync(asset, purchase: new SaveAssetDetailsCommand.PurchaseInput(
            null, "PO-1", "INV-1", new DateOnly(2026, 1, 5), 12_500m,
            new DateOnly(2026, 1, 5), new DateOnly(2029, 1, 4)));

        saved.IsSuccess.ShouldBeTrue();
        (await GetAsync(asset)).Value.PurchaseDetail.ShouldNotBeNull()
            .PurchaseCost.ShouldBe(12_500m);
    }

    [Fact]
    public async Task Book_values_are_hidden_without_the_finance_capability()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var asset = await RegisterAsync("AST-0005", "A laptop", type, status);
        await AddFinanceAsync(asset, netBookValue: 34_000m);

        (await GetAsync(asset)).Value.FinanceDetail.ShouldBeNull();

        fixture.CurrentUser.Capabilities = new HashSet<string> { "asset-finance.view" };
        try
        {
            (await GetAsync(asset)).Value.FinanceDetail.ShouldNotBeNull()
                .NetBookValue.ShouldBe(34_000m);
        }
        finally
        {
            fixture.CurrentUser.Capabilities = new HashSet<string>();
        }
    }

    [Fact]
    public async Task A_deleted_asset_still_opens_and_says_it_was_removed()
    {
        // History points at it. A 404 here reads as a broken link from a
        // movement or a ticket, which is worse than the truth.
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var asset = await RegisterAsync("AST-0006", "A laptop", type, status);
        await DeleteAsync(asset);

        var result = await GetAsync(asset);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Asset.IsDeleted.ShouldBeTrue();
    }

    // ------------------------------------------------------- detail: negative

    [Fact]
    public async Task An_unknown_asset_is_a_404()
    {
        await fixture.ResetAsync();

        (await GetAsync(987654)).Error!.Code.ShouldBe("Asset.NotFound");
        (await GetTimelineAsync(987654)).Error!.Code.ShouldBe("Asset.NotFound");
    }

    [Fact]
    public async Task An_asset_at_another_branch_is_a_404_and_not_a_403()
    {
        // Telling somebody an asset exists at a branch they cannot see is
        // itself a disclosure.
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var asset = await RegisterAsync("AST-0007", "A laptop", type, status, locationId: 2);

        fixture.CurrentUser.HasAllBranches = false;
        fixture.CurrentUser.BranchIds = new HashSet<int> { 1 };
        try
        {
            (await GetAsync(asset)).Error!.Code.ShouldBe("Asset.NotFound");
        }
        finally
        {
            fixture.CurrentUser.HasAllBranches = true;
            fixture.CurrentUser.BranchIds = new HashSet<int>();
        }
    }

    [Fact]
    public async Task A_type_that_does_not_track_a_detail_refuses_it()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var asset = await RegisterAsync("AST-0008", "A chair", type, status);

        var result = await SaveDetailsAsync(asset, hardware: new SaveAssetDetailsCommand.HardwareInput(
            "CHAIR-01", null, null, null, null, null, null, null, null));

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Asset.DetailNotTracked");
    }

    [Fact]
    public async Task A_vehicle_without_a_registration_number_is_refused()
    {
        await fixture.ResetAsync();
        var (_, status) = await SeedAsync();
        var vehicles = await CreateTypeAsync("Vehicles", tracksVehicle: true);
        var asset = await RegisterAsync("VEH-0003", "A van", vehicles, status);

        var result = await SaveDetailsAsync(asset, vehicle: new SaveAssetDetailsCommand.VehicleInput(
            "   ", null, null, null, null, null, null, null));

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Vehicle.RegistrationRequired");
    }

    [Fact]
    public async Task Two_vehicles_cannot_share_a_registration_number()
    {
        await fixture.ResetAsync();
        var (_, status) = await SeedAsync();
        var vehicles = await CreateTypeAsync("Vehicles", tracksVehicle: true);
        var first = await RegisterAsync("VEH-0004", "Van one", vehicles, status);
        var second = await RegisterAsync("VEH-0005", "Van two", vehicles, status);
        await SaveDetailsAsync(first, vehicle: Registration("TN-09-XX-9999"));

        var result = await SaveDetailsAsync(second, vehicle: Registration("TN-09-XX-9999"));

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Vehicle.RegistrationTaken");
    }

    [Fact]
    public async Task A_calibration_cannot_expire_before_it_was_issued()
    {
        await fixture.ResetAsync();
        var (_, status) = await SeedAsync();
        var meters = await CreateTypeAsync("Test Meters", tracksCalibration: true);
        var asset = await RegisterAsync("INS-0002", "A test meter", meters, status);

        var result = await SaveDetailsAsync(asset, instrument: new SaveAssetDetailsCommand.InstrumentInput(
            new DateOnly(2027, 1, 1), new DateOnly(2026, 1, 1), null, null, null, null, null));

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Instrument.CalibrationWindow");
    }

    [Fact]
    public async Task Warranty_cover_cannot_end_before_it_starts()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var asset = await RegisterAsync("AST-0009", "A laptop", type, status);

        var result = await SaveDetailsAsync(asset, purchase: new SaveAssetDetailsCommand.PurchaseInput(
            null, null, null, null, null, new DateOnly(2027, 1, 1), new DateOnly(2026, 1, 1)));

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Purchase.WarrantyWindow");
    }

    [Fact]
    public async Task Details_cannot_be_saved_against_a_deleted_asset()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var asset = await RegisterAsync("AST-0010", "A chair", type, status);
        await DeleteAsync(asset);

        var result = await SaveDetailsAsync(asset, purchase: new SaveAssetDetailsCommand.PurchaseInput(
            null, "PO-9", null, null, null, null, null));

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Asset.Deleted");
    }

    // ----------------------------------------------------------- the timeline

    [Fact]
    public async Task The_timeline_comes_back_newest_first_and_pages()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var other = (await CreateStatusAsync("Under Repair", 4)).Value.Id;
        var asset = await RegisterAsync("AST-0011", "A laptop", type, status);

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddMinutes(1);
        await UpdateStatusAsync(asset, "AST-0011", type, other);

        var page = (await GetTimelineAsync(asset)).Value;
        page.TotalCount.ShouldBe(2);
        page.Rows.Select(r => r.EventType).ShouldBe(["StatusChanged", "Registered"]);

        var second = (await GetTimelineAsync(asset, skip: 1, take: 1)).Value;
        second.Rows.Single().EventType.ShouldBe("Registered");
        second.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Entries_sharing_a_timestamp_keep_a_stable_order()
    {
        // Several modules append in one transaction, so identical timestamps
        // are normal. Without the id tiebreak the two swap between page loads.
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var other = (await CreateStatusAsync("Under Repair", 4)).Value.Id;
        var asset = await RegisterAsync("AST-0012", "A laptop", type, status);
        await UpdateStatusAsync(asset, "AST-0012", type, other);

        var first = (await GetTimelineAsync(asset)).Value.Rows.Select(r => r.Id).ToArray();
        var again = (await GetTimelineAsync(asset)).Value.Rows.Select(r => r.Id).ToArray();

        first.ShouldBe(again);
        first.ShouldBe(first.OrderByDescending(id => id).ToArray());
    }

    [Fact]
    public async Task An_asset_that_has_only_just_been_registered_has_one_entry()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var asset = await RegisterAsync("AST-0013", "A laptop", type, status);

        var page = (await GetTimelineAsync(asset)).Value;

        page.TotalCount.ShouldBe(1);
        page.Rows.Single().PerformedBy.ShouldBe("test-admin");
    }

    // -------------------------------------------------------- custom values

    [Fact]
    public async Task Custom_fields_are_listed_empty_before_anything_is_filled_in()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        await DefineFieldAsync(type, "Colour", "Colour", "Text");
        var asset = await RegisterAsync("AST-0014", "A chair", type, status);

        var values = (await GetAsync(asset)).Value.CustomValues;

        values.Single().FieldName.ShouldBe("Colour");
        values.Single().Value.ShouldBeNull();
    }

    [Fact]
    public async Task A_value_can_be_set_read_back_and_cleared()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var field = (await DefineFieldAsync(type, "Colour", "Colour", "Text")).Value.Id;
        var asset = await RegisterAsync("AST-0015", "A chair", type, status);

        (await SetValuesAsync(asset, new SetAssetCustomValuesCommand.Entry(field, "Blue", null, null, null)))
            .Value.SavedCount.ShouldBe(1);
        (await GetAsync(asset)).Value.CustomValues.Single().Value.ShouldBe("Blue");

        await SetValuesAsync(asset, new SetAssetCustomValuesCommand.Entry(field, "  ", null, null, null));
        (await GetAsync(asset)).Value.CustomValues.Single().Value.ShouldBeNull();
    }

    [Fact]
    public async Task A_number_outside_its_range_is_refused()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var field = (await DefineFieldAsync(type, "Screen", "Screen size", "Number", min: 10m, max: 20m)).Value.Id;
        var asset = await RegisterAsync("AST-0016", "A laptop", type, status);

        (await SetValuesAsync(asset, new SetAssetCustomValuesCommand.Entry(field, null, 9m, null, null)))
            .Error!.Code.ShouldBe("CustomField.BelowMinimum");
        (await SetValuesAsync(asset, new SetAssetCustomValuesCommand.Entry(field, null, 21m, null, null)))
            .Error!.Code.ShouldBe("CustomField.AboveMaximum");
        (await SetValuesAsync(asset, new SetAssetCustomValuesCommand.Entry(field, null, 15m, null, null)))
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task A_required_field_left_blank_fails_the_save()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        await DefineFieldAsync(type, "Owner", "Owner", "Text", isRequired: true);
        var asset = await RegisterAsync("AST-0017", "A chair", type, status);

        var result = await SetValuesAsync(asset);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("CustomField.Required");
    }

    [Fact]
    public async Task A_dropdown_takes_one_of_its_own_options_and_nothing_else()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var mine = (await DefineFieldAsync(type, "Condition", "Condition", "Dropdown",
            options: ["Good", "Poor"])).Value.Id;
        var theirs = (await DefineFieldAsync(type, "Other", "Other", "Dropdown",
            options: ["Alpha"])).Value.Id;
        var asset = await RegisterAsync("AST-0018", "A chair", type, status);

        var options = (await GetAsync(asset)).Value.CustomValues
            .Single(v => v.CustomFieldDefinitionId == mine).Options;
        var foreignOption = (await GetAsync(asset)).Value.CustomValues
            .Single(v => v.CustomFieldDefinitionId == theirs).Options.Single().Id;

        (await SetValuesAsync(asset,
            new SetAssetCustomValuesCommand.Entry(mine, null, null, null, options[0].Id),
            new SetAssetCustomValuesCommand.Entry(theirs, null, null, null, foreignOption)))
            .IsSuccess.ShouldBeTrue();

        // An option id from the OTHER dropdown is a valid row and the wrong answer.
        (await SetValuesAsync(asset,
            new SetAssetCustomValuesCommand.Entry(mine, null, null, null, foreignOption),
            new SetAssetCustomValuesCommand.Entry(theirs, null, null, null, foreignOption)))
            .Error!.Code.ShouldBe("CustomField.UnknownOption");
    }

    [Fact]
    public async Task A_field_belonging_to_another_type_is_refused()
    {
        // The unique index is on (AssetId, CustomFieldDefinitionId) and neither
        // column knows what type the asset is, so nothing else would catch it.
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var otherType = await CreateTypeAsync("Vehicles");
        var foreignField = (await DefineFieldAsync(otherType, "Fuel", "Fuel", "Text")).Value.Id;
        var asset = await RegisterAsync("AST-0019", "A chair", type, status);

        var result = await SetValuesAsync(
            asset, new SetAssetCustomValuesCommand.Entry(foreignField, "Diesel", null, null, null));

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("CustomField.NotOnThisType");
    }

    [Fact]
    public async Task A_date_field_wants_a_date_and_a_number_field_wants_a_number()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var dateField = (await DefineFieldAsync(type, "Installed", "Installed on", "Date")).Value.Id;
        var numberField = (await DefineFieldAsync(type, "Weight", "Weight", "Number")).Value.Id;
        var asset = await RegisterAsync("AST-0020", "A chair", type, status);

        (await SetValuesAsync(asset,
            new SetAssetCustomValuesCommand.Entry(dateField, "not a date", null, null, null)))
            .Error!.Code.ShouldBe("CustomField.DateExpected");

        (await SetValuesAsync(asset,
            new SetAssetCustomValuesCommand.Entry(numberField, "heavy", null, null, null)))
            .Error!.Code.ShouldBe("CustomField.NumberExpected");
    }

    [Fact]
    public async Task Values_cannot_be_set_on_a_deleted_asset()
    {
        await fixture.ResetAsync();
        var (type, status) = await SeedAsync();
        var field = (await DefineFieldAsync(type, "Colour", "Colour", "Text")).Value.Id;
        var asset = await RegisterAsync("AST-0021", "A chair", type, status);
        await DeleteAsync(asset);

        var result = await SetValuesAsync(
            asset, new SetAssetCustomValuesCommand.Entry(field, "Blue", null, null, null));

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Asset.Deleted");
    }

    // -------------------------------------------------------------- helpers

    private static SaveAssetDetailsCommand.VehicleInput Registration(string number) =>
        new(number, null, null, null, null, null, null, null);

    private async Task<(int Type, int Status)> SeedAsync()
    {
        var type = await CreateTypeAsync("Laptops");
        var status = (await CreateStatusAsync("In Stock", 1)).Value.Id;
        return (type, status);
    }

    private async Task<int> CreateTypeAsync(
        string name,
        bool tracksHardware = false,
        bool tracksSoftware = false,
        bool tracksVehicle = false,
        bool tracksCalibration = false)
    {
        var handler = new CreateAssetTypeHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        var result = await handler.HandleAsync(
            new CreateAssetTypeCommand(name, null, true, true, false,
                tracksHardware, tracksSoftware, tracksVehicle, tracksCalibration),
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

    private Task<Result<DefineCustomFieldResponse>> DefineFieldAsync(
        int assetTypeId, string fieldName, string label, string fieldType,
        bool isRequired = false, decimal? min = null, decimal? max = null,
        IReadOnlyList<string>? options = null)
    {
        var handler = new DefineCustomFieldHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new DefineCustomFieldCommand(
                assetTypeId, fieldName, label, fieldType, isRequired, min, max, null, null,
                0, options ?? []),
            TestContext.Current.CancellationToken);
    }

    private async Task<int> RegisterAsync(
        string number, string name, int typeId, int statusId, int? locationId = null)
    {
        var context = fixture.NewAssetsContext();
        var handler = new RegisterAssetHandler(
            context, new AssetTimeline(context), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        var result = await handler.HandleAsync(
            new RegisterAssetCommand(
                number, name, null, typeId, null, null, null, statusId,
                locationId, null, null, null, false, 1m, null, null),
            TestContext.Current.CancellationToken);
        return result.Value.Id;
    }

    private async Task UpdateStatusAsync(int id, string number, int typeId, int statusId)
    {
        var context = fixture.NewAssetsContext();
        var handler = new UpdateAssetHandler(
            context, new AssetTimeline(context), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        await handler.HandleAsync(
            new UpdateAssetCommand(
                id, number, "A laptop", null, typeId, null, null, null, statusId,
                null, null, null, null, false, 1m, null, null),
            TestContext.Current.CancellationToken);
    }

    private async Task DeleteAsync(int id)
    {
        var context = fixture.NewAssetsContext();
        var handler = new Modules.Assets.Features.DeleteAsset.DeleteAssetHandler(
            context, new AssetTimeline(context), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        await handler.HandleAsync(
            new Modules.Assets.Features.DeleteAsset.DeleteAssetCommand(id, null),
            TestContext.Current.CancellationToken);
    }

    private async Task AddFinanceAsync(int assetId, decimal netBookValue)
    {
        await using var context = fixture.NewAssetsContext();
        context.AssetFinances.Add(new Modules.Assets.Domain.AssetFinance
        {
            AssetId = assetId,
            NetBookValue = netBookValue,
            GrossValue = netBookValue * 2,
            CreatedOnUtc = fixture.Clock.UtcNow,
            CreatedBy = "sap-sync",
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private Task<Result<GetAssetResponse>> GetAsync(int id)
    {
        var handler = new GetAssetHandler(fixture.NewAssetsContext(), fixture.CurrentUser);
        return handler.HandleAsync(new GetAssetQuery(id), TestContext.Current.CancellationToken);
    }

    private Task<Result<GetAssetTimelineResponse>> GetTimelineAsync(
        int id, int skip = 0, int take = 50)
    {
        var handler = new GetAssetTimelineHandler(fixture.NewAssetsContext());
        return handler.HandleAsync(
            new GetAssetTimelineQuery(id, skip, take), TestContext.Current.CancellationToken);
    }

    private Task<Result<SaveAssetDetailsResponse>> SaveDetailsAsync(
        int assetId,
        SaveAssetDetailsCommand.HardwareInput? hardware = null,
        SaveAssetDetailsCommand.SoftwareInput? software = null,
        SaveAssetDetailsCommand.PurchaseInput? purchase = null,
        SaveAssetDetailsCommand.VehicleInput? vehicle = null,
        SaveAssetDetailsCommand.InstrumentInput? instrument = null)
    {
        var handler = new SaveAssetDetailsHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new SaveAssetDetailsCommand(assetId, hardware, software, purchase, vehicle, instrument),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SetAssetCustomValuesResponse>> SetValuesAsync(
        int assetId, params SetAssetCustomValuesCommand.Entry[] entries)
    {
        var handler = new SetAssetCustomValuesHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new SetAssetCustomValuesCommand(assetId, entries),
            TestContext.Current.CancellationToken);
    }
}
