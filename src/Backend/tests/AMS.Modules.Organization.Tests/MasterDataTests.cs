using AMS.Modules.Organization.Features.CreateDepartment;
using AMS.Modules.Organization.Features.CreateBranch;
using AMS.Modules.Organization.Features.CreateRegion;
using AMS.Modules.Organization.Features.CreateVendor;
using AMS.Modules.Organization.Features.SearchDepartments;
using AMS.Modules.Organization.Features.SearchBranches;
using AMS.Modules.Organization.Features.SearchRegions;
using AMS.Modules.Organization.Features.SearchVendors;
using AMS.Modules.Organization.Features.UpdateDepartment;
using AMS.Modules.Organization.Features.UpdateBranch;
using AMS.Modules.Organization.Features.UpdateRegion;
using AMS.Modules.Organization.Features.UpdateVendor;
using AMS.SharedKernel.Results;

namespace AMS.Modules.Organization.Tests;

/// <summary>
/// Catalogue screens: Regions, Branches, Departments, Vendors. Features:
/// Branches and branches, Departments, Vendors, Regions, Put a branch in a
/// region, Branch time zone.
/// </summary>
[Collection(nameof(OrganizationCollectionDefinition))]
public sealed class MasterDataTests(OrganizationFixture fixture)
{
    private const string TimeZone = "India Standard Time";

    // ------------------------------------------------------------- Regions

    [Fact]
    public async Task A_region_can_be_created_and_listed()
    {
        await fixture.ResetAsync();

        var created = await CreateRegionAsync("North");

        created.IsSuccess.ShouldBeTrue();
        (await SearchRegionsAsync(null)).Value.Rows.Single().RegionName.ShouldBe("North");
    }

    [Fact]
    public async Task Two_regions_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        await CreateRegionAsync("South");

        var result = await CreateRegionAsync("South");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Region.NameTaken");
    }

    [Fact]
    public async Task A_region_counts_the_branches_in_it()
    {
        await fixture.ResetAsync();
        var region = await CreateRegionAsync("West");
        await CreateBranchAsync("BLR", "Bangalore", region.Value.Id);
        await CreateBranchAsync("PNQ", "Pune", region.Value.Id);

        (await SearchRegionsAsync(null)).Value.Rows.Single().BranchCount.ShouldBe(2);
    }

    [Fact]
    public async Task An_unknown_region_cannot_be_updated()
    {
        await fixture.ResetAsync();

        (await UpdateRegionAsync(999, "Ghost", true)).Error!.Code.ShouldBe("Region.NotFound");
    }

    [Fact]
    public async Task A_region_is_retired_by_deactivation_not_deletion()
    {
        // Branches still point at it; deleting would orphan them.
        await fixture.ResetAsync();
        var region = await CreateRegionAsync("Retiring");

        await UpdateRegionAsync(region.Value.Id, "Retiring", isActive: false);

        (await SearchRegionsAsync(false)).Value.Rows.Count.ShouldBe(1);
        (await SearchRegionsAsync(true)).Value.Rows.ShouldBeEmpty();
    }

    // ------------------------------------------------------------ Branches

    [Fact]
    public async Task A_branch_can_be_created_with_a_region_and_a_time_zone()
    {
        await fixture.ResetAsync();
        var region = await CreateRegionAsync("North");

        var result = await CreateBranchAsync("DEL", "Delhi", region.Value.Id);

        result.IsSuccess.ShouldBeTrue();
        var row = (await SearchBranchesAsync()).Value.Rows.Single();
        row.RegionName.ShouldBe("North");
        row.TimeZoneId.ShouldBe(TimeZone);
    }

    [Fact]
    public async Task A_branch_code_is_upper_cased_so_case_cannot_duplicate_it()
    {
        await fixture.ResetAsync();
        await CreateBranchAsync("del", "Delhi", null);

        var result = await CreateBranchAsync("DEL", "Delhi Again", null);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Branch.CodeTaken");
    }

    [Fact]
    public async Task Only_one_branch_can_be_head_office()
    {
        // UX_Branch_OneHeadOffice makes a second one impossible rather than
        // merely unlikely.
        await fixture.ResetAsync();
        await CreateBranchAsync("HO1", "Head Office", null, isHeadOffice: true);

        var result = await CreateBranchAsync("HO2", "Another Head Office", null, isHeadOffice: true);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Branch.HeadOfficeExists");
        result.Error.Kind.ShouldBe(ErrorKind.Conflict);
    }

    [Fact]
    public async Task Many_branches_can_be_not_head_office()
    {
        // The index is FILTERED; without the filter the second false would
        // collide with the first.
        await fixture.ResetAsync();

        (await CreateBranchAsync("B1", "One", null)).IsSuccess.ShouldBeTrue();
        (await CreateBranchAsync("B2", "Two", null)).IsSuccess.ShouldBeTrue();
        (await CreateBranchAsync("B3", "Three", null)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Moving_head_office_requires_clearing_the_old_one_first()
    {
        await fixture.ResetAsync();
        var first = await CreateBranchAsync("HO1", "Head Office", null, isHeadOffice: true);
        var second = await CreateBranchAsync("BR2", "Branch", null);

        // Straight to the new one: refused, because two would be flagged.
        var refused = await UpdateBranchAsync(second.Value.Id, "BR2", "Branch", isHeadOffice: true);
        refused.Error!.Code.ShouldBe("Branch.HeadOfficeExists");

        // Clear, then set. Now it works.
        await UpdateBranchAsync(first.Value.Id, "HO1", "Head Office", isHeadOffice: false);
        (await UpdateBranchAsync(second.Value.Id, "BR2", "Branch", isHeadOffice: true)).IsSuccess.ShouldBeTrue();

        (await fixture.ScalarAsync<int>(
            "SELECT COUNT(*) FROM [Organization].[Branch] WHERE [IsHeadOffice] = 1;")).ShouldBe(1);
    }

    [Fact]
    public async Task A_branch_can_be_moved_between_regions()
    {
        await fixture.ResetAsync();
        var north = await CreateRegionAsync("North");
        var south = await CreateRegionAsync("South");
        var branch = await CreateBranchAsync("MAA", "Chennai", north.Value.Id);

        await UpdateBranchAsync(branch.Value.Id, "MAA", "Chennai", regionId: south.Value.Id);

        (await SearchBranchesAsync(regionId: south.Value.Id)).Value.Rows.Single()
            .BranchName.ShouldBe("Chennai");
        (await SearchBranchesAsync(regionId: north.Value.Id)).Value.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_branch_with_no_region_is_allowed()
    {
        // Opening a branch before somebody decides its region is normal.
        await fixture.ResetAsync();

        var result = await CreateBranchAsync("TMP", "Temporary", null);

        result.IsSuccess.ShouldBeTrue();
        (await SearchBranchesAsync()).Value.Rows.Single().RegionName.ShouldBeNull();
    }

    // --------------------------------------------------------- Departments

    [Fact]
    public async Task A_department_can_be_created_renamed_and_deactivated()
    {
        await fixture.ResetAsync();
        var created = await CreateDepartmentAsync("Finance");

        var renamed = await UpdateDepartmentAsync(created.Value.Id, "Finance and Accounts", true);
        renamed.Value.DepartmentName.ShouldBe("Finance and Accounts");

        var retired = await UpdateDepartmentAsync(created.Value.Id, "Finance and Accounts", false);
        retired.Value.IsActive.ShouldBeFalse();

        await using var context = fixture.NewContext();
        var listed = await new SearchDepartmentsHandler(context)
            .HandleAsync(new SearchDepartmentsQuery(null, null), TestContext.Current.CancellationToken);
        listed.Value.Rows.Single().DepartmentName.ShouldBe("Finance and Accounts");
    }

    [Fact]
    public async Task Two_departments_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        await CreateDepartmentAsync("IT");

        (await CreateDepartmentAsync("IT")).Error!.Code.ShouldBe("Department.NameTaken");
    }

    // ------------------------------------------------------------- Vendors

    [Fact]
    public async Task A_vendor_can_be_created_with_contact_details()
    {
        await fixture.ResetAsync();

        var result = await CreateVendorAsync("Acme Supplies");

        result.IsSuccess.ShouldBeTrue();
        var row = (await SearchVendorsAsync()).Value.Rows.Single();
        row.ContactPerson.ShouldBe("A Person");
        row.Email.ShouldBe("sales@acme.example");
    }

    [Fact]
    public async Task Two_vendors_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        await CreateVendorAsync("Acme Supplies");

        (await CreateVendorAsync("Acme Supplies")).Error!.Code.ShouldBe("Vendor.NameTaken");
    }

    [Fact]
    public async Task A_vendor_can_be_edited_and_retired()
    {
        await fixture.ResetAsync();
        var created = await CreateVendorAsync("Acme Supplies");

        await using var context = fixture.NewContext();
        var result = await new UpdateVendorHandler(
                context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(
                new UpdateVendorCommand(
                    created.Value.Id, "Acme Supplies Ltd", "Someone Else", null, null, false),
                TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.VendorName.ShouldBe("Acme Supplies Ltd");
        result.Value.IsActive.ShouldBeFalse();

        // Contact details are replaced wholesale, including being cleared.
        var row = (await SearchVendorsAsync()).Value.Rows.Single();
        row.ContactPerson.ShouldBe("Someone Else");
        row.Email.ShouldBeNull();
    }

    [Fact]
    public async Task An_unknown_vendor_cannot_be_edited()
    {
        await fixture.ResetAsync();

        await using var context = fixture.NewContext();
        var result = await new UpdateVendorHandler(
                context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(
                new UpdateVendorCommand(4321, "Ghost", null, null, null, true),
                TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("Vendor.NotFound");
    }

    [Fact]
    public async Task A_vendor_search_matches_the_contact_person_too()
    {
        await fixture.ResetAsync();
        await CreateVendorAsync("Acme Supplies");

        (await SearchVendorsAsync("A Person")).Value.Rows.Count.ShouldBe(1);
        (await SearchVendorsAsync("nobody")).Value.Rows.ShouldBeEmpty();
    }

    // ------------------------------------------------------------ helpers

    private async Task<Result<CreateRegionResponse>> CreateRegionAsync(string name)
    {
        await using var context = fixture.NewContext();
        return await new CreateRegionHandler(context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(new CreateRegionCommand(name, null), TestContext.Current.CancellationToken);
    }

    private async Task<Result<UpdateRegionResponse>> UpdateRegionAsync(int id, string name, bool isActive)
    {
        await using var context = fixture.NewContext();
        return await new UpdateRegionHandler(context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(new UpdateRegionCommand(id, name, null, isActive), TestContext.Current.CancellationToken);
    }

    private async Task<Result<SearchRegionsResponse>> SearchRegionsAsync(bool? isActive)
    {
        await using var context = fixture.NewContext();
        return await new SearchRegionsHandler(context)
            .HandleAsync(new SearchRegionsQuery(isActive, null), TestContext.Current.CancellationToken);
    }

    private async Task<Result<CreateBranchResponse>> CreateBranchAsync(
        string code, string name, int? regionId, bool isHeadOffice = false)
    {
        await using var context = fixture.NewContext();
        return await new CreateBranchHandler(context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(
                new CreateBranchCommand(code.ToUpperInvariant(), name, regionId, null, null, TimeZone, isHeadOffice),
                TestContext.Current.CancellationToken);
    }

    private async Task<Result<UpdateBranchResponse>> UpdateBranchAsync(
        int id,
        string code,
        string name,
        bool isHeadOffice = false,
        int? regionId = null,
        bool isActive = true)
    {
        await using var context = fixture.NewContext();
        return await new UpdateBranchHandler(context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(
                new UpdateBranchCommand(id, code, name, regionId, null, null, TimeZone, isHeadOffice, isActive),
                TestContext.Current.CancellationToken);
    }

    private async Task<Result<SearchBranchesResponse>> SearchBranchesAsync(int? regionId = null)
    {
        await using var context = fixture.NewContext();
        return await new SearchBranchesHandler(context)
            .HandleAsync(new SearchBranchesQuery(null, regionId, null), TestContext.Current.CancellationToken);
    }

    private async Task<Result<CreateDepartmentResponse>> CreateDepartmentAsync(string name)
    {
        await using var context = fixture.NewContext();
        return await new CreateDepartmentHandler(context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(new CreateDepartmentCommand(name), TestContext.Current.CancellationToken);
    }

    private async Task<Result<UpdateDepartmentResponse>> UpdateDepartmentAsync(int id, string name, bool isActive)
    {
        await using var context = fixture.NewContext();
        return await new UpdateDepartmentHandler(context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(new UpdateDepartmentCommand(id, name, isActive), TestContext.Current.CancellationToken);
    }

    private async Task<Result<CreateVendorResponse>> CreateVendorAsync(string name)
    {
        await using var context = fixture.NewContext();
        return await new CreateVendorHandler(context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(
                new CreateVendorCommand(name, "A Person", "+91 80 1234 5678", "sales@acme.example"),
                TestContext.Current.CancellationToken);
    }

    private async Task<Result<SearchVendorsResponse>> SearchVendorsAsync(string? search = null)
    {
        await using var context = fixture.NewContext();
        return await new SearchVendorsHandler(context)
            .HandleAsync(new SearchVendorsQuery(null, search), TestContext.Current.CancellationToken);
    }
}
