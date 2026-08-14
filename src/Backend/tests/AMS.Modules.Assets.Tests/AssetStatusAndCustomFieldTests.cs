using AMS.Modules.Assets.Features.CreateAssetStatus;
using AMS.Modules.Assets.Features.CreateAssetType;
using AMS.Modules.Assets.Features.DefineCustomField;
using AMS.Modules.Assets.Features.GetAssetTypeCustomFields;
using AMS.Modules.Assets.Features.SearchAssetStatuses;
using AMS.Modules.Assets.Features.UpdateAssetStatus;
using AMS.Modules.Assets.Features.UpdateCustomField;
using AMS.SharedKernel.Results;

namespace AMS.Modules.Assets.Tests;

/// <summary>
/// Catalogue screens: Asset Statuses, and the custom field half of Asset Types
/// and Custom Fields. Features: "Define custom fields", "Fill custom fields",
/// "Status lookup maintenance".
/// </summary>
[Collection(nameof(AssetsCollectionDefinition))]
public sealed class AssetStatusAndCustomFieldTests(AssetsFixture fixture)
{
    // ---------------------------------------------------- statuses: positive

    [Fact]
    public async Task A_status_can_be_created_and_listed()
    {
        await fixture.ResetAsync();

        var created = await CreateStatusAsync("In Stock", isTerminal: false, displayOrder: 1);

        created.IsSuccess.ShouldBeTrue();
        var row = (await SearchStatusesAsync(null)).Value.Rows.Single();
        row.StatusName.ShouldBe("In Stock");
        row.IsTerminal.ShouldBeFalse();
        row.AssetCount.ShouldBe(0);
    }

    [Fact]
    public async Task Statuses_come_back_in_display_order()
    {
        await fixture.ResetAsync();
        await CreateStatusAsync("Scrapped", isTerminal: true, displayOrder: 20);
        await CreateStatusAsync("In Stock", isTerminal: false, displayOrder: 1);
        await CreateStatusAsync("Allocated", isTerminal: false, displayOrder: 2);

        (await SearchStatusesAsync(null)).Value.Rows
            .Select(r => r.StatusName)
            .ShouldBe(["In Stock", "Allocated", "Scrapped"]);
    }

    [Fact]
    public async Task Statuses_sharing_a_display_order_still_come_back_in_a_stable_sequence()
    {
        // Two rows at the same order must not depend on insertion order, or the
        // picker reshuffles itself between page loads.
        await fixture.ResetAsync();
        await CreateStatusAsync("Zulu", isTerminal: false, displayOrder: 5);
        await CreateStatusAsync("Alpha", isTerminal: false, displayOrder: 5);

        (await SearchStatusesAsync(null)).Value.Rows
            .Select(r => r.StatusName)
            .ShouldBe(["Alpha", "Zulu"]);
    }

    [Fact]
    public async Task A_status_can_be_renamed_reordered_and_retired()
    {
        await fixture.ResetAsync();
        var created = await CreateStatusAsync("Temp", isTerminal: false, displayOrder: 9);

        var updated = await UpdateStatusAsync(
            created.Value.Id, "Retired Status", isTerminal: true, displayOrder: 30, isActive: false);

        updated.IsSuccess.ShouldBeTrue();
        var row = (await SearchStatusesAsync(null)).Value.Rows.Single();
        row.StatusName.ShouldBe("Retired Status");
        row.IsTerminal.ShouldBeTrue();
        row.DisplayOrder.ShouldBe(30);
        row.IsActive.ShouldBeFalse();
    }

    // ---------------------------------------------------- statuses: negative

    [Fact]
    public async Task Two_statuses_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        await CreateStatusAsync("Allocated", isTerminal: false, displayOrder: 2);

        var result = await CreateStatusAsync("Allocated", isTerminal: false, displayOrder: 3);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("AssetStatus.NameTaken");
    }

    [Fact]
    public async Task An_unknown_status_cannot_be_updated()
    {
        await fixture.ResetAsync();

        (await UpdateStatusAsync(987654, "Ghost", false, 1)).Error!.Code
            .ShouldBe("AssetStatus.NotFound");
    }

    // -------------------------------------------------------- statuses: edge

    [Fact]
    public async Task A_status_with_assets_in_it_cannot_be_retired()
    {
        // Nothing in the database stops this: the assets keep the id. But the
        // status vanishes from every picker, so the only way out of it would be
        // a script.
        await fixture.ResetAsync();
        await fixture.AddAssetAsync();
        var status = (await SearchStatusesAsync(null)).Value.Rows.Single();
        status.AssetCount.ShouldBe(1);

        var result = await UpdateStatusAsync(
            status.Id, status.StatusName, status.IsTerminal, status.DisplayOrder, isActive: false);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("AssetStatus.InUse");
    }

    [Fact]
    public async Task A_status_with_assets_in_it_can_still_be_renamed()
    {
        // The guard is about retiring, not about editing.
        await fixture.ResetAsync();
        await fixture.AddAssetAsync();
        var status = (await SearchStatusesAsync(null)).Value.Rows.Single();

        var result = await UpdateStatusAsync(
            status.Id, "A Better Name", status.IsTerminal, status.DisplayOrder, isActive: true);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task An_already_inactive_status_can_be_saved_again()
    {
        // The guard fires on the transition, not on the state, or an inactive
        // status could never be edited at all.
        await fixture.ResetAsync();
        var created = await CreateStatusAsync("Dormant", isTerminal: false, displayOrder: 7);
        await UpdateStatusAsync(created.Value.Id, "Dormant", false, 7, isActive: false);

        var result = await UpdateStatusAsync(created.Value.Id, "Dormant", false, 8, isActive: false);

        result.IsSuccess.ShouldBeTrue();
    }

    // ------------------------------------------------ custom fields: positive

    [Fact]
    public async Task A_text_field_can_be_defined_and_read_back()
    {
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Laptops");

        var defined = await DefineFieldAsync(type, "AssetTag", "Asset Tag", "Text");

        defined.IsSuccess.ShouldBeTrue();
        var row = (await GetFieldsAsync(type)).Value.Rows.Single();
        row.FieldName.ShouldBe("AssetTag");
        row.FieldType.ShouldBe("Text");
        row.Options.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_dropdown_keeps_its_options_in_order()
    {
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Laptops");

        await DefineFieldAsync(type, "Condition", "Condition", "Dropdown",
            options: ["Good", "Fair", "Poor"]);

        (await GetFieldsAsync(type)).Value.Rows.Single().Options
            .ShouldBe(["Good", "Fair", "Poor"]);
    }

    [Fact]
    public async Task Fields_come_back_in_display_order()
    {
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Laptops");
        await DefineFieldAsync(type, "Third", "Third", "Text", displayOrder: 3);
        await DefineFieldAsync(type, "First", "First", "Text", displayOrder: 1);
        await DefineFieldAsync(type, "Second", "Second", "Text", displayOrder: 2);

        (await GetFieldsAsync(type)).Value.Rows
            .Select(r => r.FieldName)
            .ShouldBe(["First", "Second", "Third"]);
    }

    [Fact]
    public async Task A_field_can_be_edited_and_retired()
    {
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Laptops");
        var defined = await DefineFieldAsync(type, "Warranty", "Warranty", "Number");

        var updated = await UpdateFieldAsync(
            defined.Value.Id, "Warranty (months)", isRequired: true, isActive: false);

        updated.IsSuccess.ShouldBeTrue();
        (await GetFieldsAsync(type)).Value.Rows.ShouldBeEmpty();

        var withInactive = (await GetFieldsAsync(type, includeInactive: true)).Value.Rows.Single();
        withInactive.DisplayLabel.ShouldBe("Warranty (months)");
        withInactive.IsRequired.ShouldBeTrue();
        withInactive.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task A_number_field_keeps_its_range()
    {
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Laptops");

        await DefineFieldAsync(type, "Screen", "Screen size", "Number", min: 10m, max: 20m);

        var row = (await GetFieldsAsync(type)).Value.Rows.Single();
        row.MinValue.ShouldBe(10m);
        row.MaxValue.ShouldBe(20m);
    }

    // ------------------------------------------------ custom fields: negative

    [Fact]
    public async Task A_field_cannot_be_defined_on_a_type_that_does_not_exist()
    {
        await fixture.ResetAsync();

        var result = await DefineFieldAsync(987654, "Orphan", "Orphan", "Text");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("AssetType.NotFound");
    }

    [Fact]
    public async Task Two_fields_on_one_type_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Laptops");
        await DefineFieldAsync(type, "Colour", "Colour", "Text");

        var result = await DefineFieldAsync(type, "Colour", "Colour again", "Text");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("CustomField.NameTaken");
    }

    [Fact]
    public async Task An_unknown_field_type_is_refused()
    {
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Laptops");

        var result = await DefineFieldAsync(type, "Weird", "Weird", "Hologram");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("CustomField.UnknownType");
    }

    [Fact]
    public async Task The_fields_of_a_type_that_does_not_exist_are_a_404()
    {
        await fixture.ResetAsync();

        (await GetFieldsAsync(987654)).Error!.Code.ShouldBe("AssetType.NotFound");
    }

    [Fact]
    public async Task An_unknown_field_cannot_be_updated()
    {
        await fixture.ResetAsync();

        (await UpdateFieldAsync(987654, "Ghost")).Error!.Code
            .ShouldBe("CustomFieldDefinition.NotFound");
    }

    // ---------------------------------------------------- custom fields: edge

    [Fact]
    public async Task A_dropdown_with_no_options_is_refused()
    {
        // An empty picker cannot be satisfied, and if the field is also
        // required the asset can never be saved at all.
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Laptops");

        var result = await DefineFieldAsync(type, "Empty", "Empty", "Dropdown", options: []);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("CustomField.DropdownNeedsOptions");
    }

    [Fact]
    public async Task Options_on_a_field_that_is_not_a_dropdown_are_refused()
    {
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Laptops");

        var result = await DefineFieldAsync(
            type, "Notes", "Notes", "Text", options: ["A", "B"]);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("CustomField.OptionsNotAllowed");
    }

    [Fact]
    public async Task Two_options_with_the_same_value_are_refused()
    {
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Laptops");

        var result = await DefineFieldAsync(
            type, "Condition", "Condition", "Dropdown", options: ["Good", "good"]);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("CustomField.DuplicateOption");
    }

    [Fact]
    public async Task Blank_options_are_dropped_rather_than_stored()
    {
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Laptops");

        await DefineFieldAsync(
            type, "Condition", "Condition", "Dropdown", options: ["Good", "  ", "Poor"]);

        (await GetFieldsAsync(type)).Value.Rows.Single().Options.ShouldBe(["Good", "Poor"]);
    }

    [Fact]
    public async Task A_dropdown_whose_options_are_all_blank_is_refused()
    {
        // Trimming happens before the emptiness check, or a picker of three
        // spaces would count as three options.
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Laptops");

        var result = await DefineFieldAsync(
            type, "Ghost", "Ghost", "Dropdown", options: ["  ", "\t"]);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("CustomField.DropdownNeedsOptions");
    }

    [Fact]
    public async Task A_failed_dropdown_leaves_no_definition_behind()
    {
        // The definition and its options are saved in one transaction. A
        // Dropdown that exists without the values it promised would be a field
        // no asset could ever be saved against, and it would survive retries.
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Laptops");

        var result = await DefineFieldAsync(
            type, "Condition", "Condition", "Dropdown", options: ["Same", "SAME"]);

        result.IsSuccess.ShouldBeFalse();
        (await GetFieldsAsync(type, includeInactive: true)).Value.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_same_field_name_may_be_used_on_a_different_type()
    {
        // The unique index is per type, not global: "Colour" is a reasonable
        // field on both a laptop and a vehicle.
        await fixture.ResetAsync();
        var laptops = await CreateTypeAsync("Laptops");
        var vehicles = await CreateTypeAsync("Vehicles");
        await DefineFieldAsync(laptops, "Colour", "Colour", "Text");

        var result = await DefineFieldAsync(vehicles, "Colour", "Colour", "Text");

        result.IsSuccess.ShouldBeTrue();
        (await GetFieldsAsync(laptops)).Value.Rows.Count.ShouldBe(1);
        (await GetFieldsAsync(vehicles)).Value.Rows.Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_type_with_no_fields_returns_an_empty_list()
    {
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Bare");

        var result = await GetFieldsAsync(type);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AssetTypeId.ShouldBe(type);
        result.Value.Rows.ShouldBeEmpty();
    }

    // -------------------------------------------------------------- helpers

    private async Task<int> CreateTypeAsync(string name)
    {
        var handler = new CreateAssetTypeHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        var result = await handler.HandleAsync(
            new CreateAssetTypeCommand(name, null, true, true, false, false, false, false, false),
            TestContext.Current.CancellationToken);
        return result.Value.Id;
    }

    private Task<Result<CreateAssetStatusResponse>> CreateStatusAsync(
        string name, bool isTerminal, int displayOrder)
    {
        var handler = new CreateAssetStatusHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new CreateAssetStatusCommand(name, isTerminal, displayOrder),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<UpdateAssetStatusResponse>> UpdateStatusAsync(
        int id, string name, bool isTerminal, int displayOrder, bool isActive = true)
    {
        var handler = new UpdateAssetStatusHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new UpdateAssetStatusCommand(id, name, isTerminal, displayOrder, isActive),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchAssetStatusesResponse>> SearchStatusesAsync(bool? isActive)
    {
        var handler = new SearchAssetStatusesHandler(fixture.NewAssetsContext());
        return handler.HandleAsync(
            new SearchAssetStatusesQuery(isActive), TestContext.Current.CancellationToken);
    }

    private Task<Result<DefineCustomFieldResponse>> DefineFieldAsync(
        int assetTypeId,
        string fieldName,
        string label,
        string fieldType,
        decimal? min = null,
        decimal? max = null,
        int displayOrder = 0,
        IReadOnlyList<string>? options = null)
    {
        var handler = new DefineCustomFieldHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new DefineCustomFieldCommand(
                assetTypeId, fieldName, label, fieldType, false, min, max, null, null,
                displayOrder, options ?? []),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<UpdateCustomFieldResponse>> UpdateFieldAsync(
        int id, string label, bool isRequired = false, bool isActive = true)
    {
        var handler = new UpdateCustomFieldHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new UpdateCustomFieldCommand(id, label, isRequired, null, null, null, null, 0, isActive),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<GetAssetTypeCustomFieldsResponse>> GetFieldsAsync(
        int assetTypeId, bool includeInactive = false)
    {
        var handler = new GetAssetTypeCustomFieldsHandler(fixture.NewAssetsContext());
        return handler.HandleAsync(
            new GetAssetTypeCustomFieldsQuery(assetTypeId, includeInactive),
            TestContext.Current.CancellationToken);
    }
}
