using AMS.Modules.Assets.Features.CreateAssetType;
using AMS.Modules.Assets.Features.SearchAssetTypes;
using AMS.Modules.Assets.Features.UpdateAssetType;
using AMS.SharedKernel.Results;

namespace AMS.Modules.Assets.Tests;

/// <summary>
/// Catalogue screen: Asset Types and Custom Fields. Feature: "Say what a type
/// of asset can do."
/// </summary>
/// <remarks>
/// The behaviour flags are the reason Revision 3 renamed this table. What an
/// asset type can do is data, so adding "Barricade" is an administrator's job
/// and not a release — which is only true if the flags survive a round trip.
/// </remarks>
[Collection(nameof(AssetsCollectionDefinition))]
public sealed class AssetTaxonomyTests(AssetsFixture fixture)
{
    // ------------------------------------------------------------- positive

    [Fact]
    public async Task A_type_can_be_created_and_listed()
    {
        await fixture.ResetAsync();

        var created = await CreateTypeAsync("Laptops");

        created.IsSuccess.ShouldBeTrue();
        var rows = (await SearchTypesAsync(null)).Value.Rows;
        rows.Single().TypeName.ShouldBe("Laptops");
        rows.Single().IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task The_seven_behaviour_flags_survive_a_round_trip()
    {
        await fixture.ResetAsync();

        await CreateTypeAsync(
            "Calibrated Instruments",
            isAllocatable: false,
            isPhysical: true,
            isBulkDefault: true,
            tracksHardware: false,
            tracksSoftware: false,
            tracksVehicle: false,
            tracksCalibration: true);

        var row = (await SearchTypesAsync(null)).Value.Rows.Single();
        row.IsAllocatable.ShouldBeFalse();
        row.IsPhysical.ShouldBeTrue();
        row.IsBulkDefault.ShouldBeTrue();
        row.TracksHardware.ShouldBeFalse();
        row.TracksSoftware.ShouldBeFalse();
        row.TracksVehicle.ShouldBeFalse();
        row.TracksCalibration.ShouldBeTrue();
    }

    [Fact]
    public async Task A_type_can_sit_under_another()
    {
        await fixture.ResetAsync();
        var parent = await CreateTypeAsync("IT Equipment");

        var child = await CreateTypeAsync("Laptops", parentId: parent.Value.Id);

        child.IsSuccess.ShouldBeTrue();
        var rows = (await SearchTypesAsync(null)).Value.Rows;
        rows.Single(r => r.TypeName == "Laptops").ParentAssetTypeId.ShouldBe(parent.Value.Id);
        rows.Single(r => r.TypeName == "IT Equipment").ParentAssetTypeId.ShouldBeNull();
    }

    [Fact]
    public async Task A_type_can_be_renamed_reflagged_and_retired()
    {
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Desktops");

        var updated = await UpdateTypeAsync(
            type.Value.Id, "Workstations", tracksHardware: true, isActive: false);

        updated.IsSuccess.ShouldBeTrue();
        var row = (await SearchTypesAsync(null)).Value.Rows.Single();
        row.TypeName.ShouldBe("Workstations");
        row.TracksHardware.ShouldBeTrue();
        row.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task The_active_filter_selects_both_ways()
    {
        await fixture.ResetAsync();
        var live = await CreateTypeAsync("Live");
        var dead = await CreateTypeAsync("Dead");
        await UpdateTypeAsync(dead.Value.Id, "Dead", isActive: false);

        (await SearchTypesAsync(true)).Value.Rows.Single().Id.ShouldBe(live.Value.Id);
        (await SearchTypesAsync(false)).Value.Rows.Single().Id.ShouldBe(dead.Value.Id);
        (await SearchTypesAsync(null)).Value.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public async Task A_type_counts_the_assets_and_custom_fields_that_point_at_it()
    {
        await fixture.ResetAsync();
        await fixture.AddAssetAsync();

        // AddAssetAsync creates exactly one type and hangs the asset off it.
        var row = (await SearchTypesAsync(null)).Value.Rows.Single();
        row.AssetCount.ShouldBe(1);
        row.CustomFieldCount.ShouldBe(0);
    }

    // ------------------------------------------------------------- negative

    [Fact]
    public async Task Two_types_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        await CreateTypeAsync("Printers");

        var result = await CreateTypeAsync("Printers");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("AssetType.NameTaken");
    }

    [Fact]
    public async Task A_type_cannot_be_created_under_a_parent_that_does_not_exist()
    {
        await fixture.ResetAsync();

        var result = await CreateTypeAsync("Orphan", parentId: 987654);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("AssetType.NotFound");
    }

    [Fact]
    public async Task An_unknown_type_cannot_be_updated()
    {
        await fixture.ResetAsync();

        (await UpdateTypeAsync(987654, "Ghost")).Error!.Code.ShouldBe("AssetType.NotFound");
    }

    [Fact]
    public async Task Renaming_a_type_onto_another_name_is_refused()
    {
        await fixture.ResetAsync();
        await CreateTypeAsync("Monitors");
        var second = await CreateTypeAsync("Keyboards");

        var result = await UpdateTypeAsync(second.Value.Id, "Monitors");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("AssetType.NameTaken");
    }

    // ----------------------------------------------------------------- edge

    [Fact]
    public async Task A_type_cannot_be_its_own_parent()
    {
        await fixture.ResetAsync();
        var type = await CreateTypeAsync("Recursive");

        var result = await UpdateTypeAsync(type.Value.Id, "Recursive", parentId: type.Value.Id);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("AssetType.ParentIsSelf");
    }

    [Fact]
    public async Task A_type_cannot_be_moved_under_its_own_descendant()
    {
        // The self-referencing FK is satisfied by A -> B -> A, so nothing in the
        // database stops this. Every screen that renders the tree recurses, so
        // the loop is a hang rather than an odd-looking list.
        await fixture.ResetAsync();
        var grandparent = await CreateTypeAsync("Equipment");
        var parent = await CreateTypeAsync("Tools", parentId: grandparent.Value.Id);
        var child = await CreateTypeAsync("Spanners", parentId: parent.Value.Id);

        var result = await UpdateTypeAsync(
            grandparent.Value.Id, "Equipment", parentId: child.Value.Id);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("AssetType.ParentIsDescendant");
    }

    [Fact]
    public async Task A_type_can_still_be_moved_sideways()
    {
        // The guard above must not refuse legitimate re-parenting.
        await fixture.ResetAsync();
        var first = await CreateTypeAsync("Branch One");
        var second = await CreateTypeAsync("Branch Two");
        var leaf = await CreateTypeAsync("Leaf", parentId: first.Value.Id);

        var result = await UpdateTypeAsync(leaf.Value.Id, "Leaf", parentId: second.Value.Id);

        result.IsSuccess.ShouldBeTrue();
        (await SearchTypesAsync(null)).Value.Rows
            .Single(r => r.TypeName == "Leaf").ParentAssetTypeId.ShouldBe(second.Value.Id);
    }

    [Fact]
    public async Task A_type_can_be_lifted_back_to_the_root()
    {
        await fixture.ResetAsync();
        var parent = await CreateTypeAsync("Parent");
        var child = await CreateTypeAsync("Child", parentId: parent.Value.Id);

        var result = await UpdateTypeAsync(child.Value.Id, "Child", parentId: null);

        result.IsSuccess.ShouldBeTrue();
        (await SearchTypesAsync(null)).Value.Rows
            .Single(r => r.TypeName == "Child").ParentAssetTypeId.ShouldBeNull();
    }

    [Fact]
    public async Task A_name_is_trimmed_before_it_is_stored()
    {
        await fixture.ResetAsync();

        await CreateTypeAsync("  Padded  ");

        (await SearchTypesAsync(null)).Value.Rows.Single().TypeName.ShouldBe("Padded");
    }

    [Fact]
    public async Task An_empty_list_is_an_empty_list_and_not_a_failure()
    {
        await fixture.ResetAsync();

        var result = await SearchTypesAsync(null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Rows.ShouldBeEmpty();
    }

    // -------------------------------------------------------------- helpers

    private Task<Result<CreateAssetTypeResponse>> CreateTypeAsync(
        string name,
        int? parentId = null,
        bool isAllocatable = true,
        bool isPhysical = true,
        bool isBulkDefault = false,
        bool tracksHardware = false,
        bool tracksSoftware = false,
        bool tracksVehicle = false,
        bool tracksCalibration = false)
    {
        var handler = new CreateAssetTypeHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new CreateAssetTypeCommand(
                name.Trim(), parentId, isAllocatable, isPhysical, isBulkDefault,
                tracksHardware, tracksSoftware, tracksVehicle, tracksCalibration),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<UpdateAssetTypeResponse>> UpdateTypeAsync(
        int id,
        string name,
        int? parentId = null,
        bool isAllocatable = true,
        bool isPhysical = true,
        bool isBulkDefault = false,
        bool tracksHardware = false,
        bool tracksSoftware = false,
        bool tracksVehicle = false,
        bool tracksCalibration = false,
        bool isActive = true)
    {
        var handler = new UpdateAssetTypeHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new UpdateAssetTypeCommand(
                id, name.Trim(), parentId, isAllocatable, isPhysical, isBulkDefault,
                tracksHardware, tracksSoftware, tracksVehicle, tracksCalibration, isActive),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchAssetTypesResponse>> SearchTypesAsync(bool? isActive)
    {
        var handler = new SearchAssetTypesHandler(fixture.NewAssetsContext());
        return handler.HandleAsync(
            new SearchAssetTypesQuery(isActive), TestContext.Current.CancellationToken);
    }
}
