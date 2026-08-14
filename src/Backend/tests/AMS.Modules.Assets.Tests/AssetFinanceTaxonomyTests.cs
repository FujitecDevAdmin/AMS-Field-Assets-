using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Features.CreateAssetClass;
using AMS.Modules.Assets.Features.CreateChartOfAccount;
using AMS.Modules.Assets.Features.SearchAssetClasses;
using AMS.Modules.Assets.Features.SearchChartOfAccounts;
using AMS.Modules.Assets.Features.UpdateAssetClass;
using AMS.Modules.Assets.Features.UpdateChartOfAccount;
using AMS.SharedKernel.Results;

namespace AMS.Modules.Assets.Tests;

/// <summary>
/// Catalogue screen: Asset Classes and Chart of Accounts. Feature: "Classify an
/// asset for the accounts."
/// </summary>
/// <remarks>
/// The class axis is separate from the type axis on purpose: 86 technical
/// groups on the live register appear under more than one class, so a single
/// tree would misfile hundreds of rows on import.
/// </remarks>
[Collection(nameof(AssetsCollectionDefinition))]
public sealed class AssetFinanceTaxonomyTests(AssetsFixture fixture)
{
    // ---------------------------------------------------- classes: positive

    [Fact]
    public async Task A_class_can_be_created_and_listed()
    {
        await fixture.ResetAsync();

        var created = await CreateClassAsync("F & F", "Furniture & Fixtures", "Furniture & Fixtures");

        created.IsSuccess.ShouldBeTrue();
        var row = (await SearchClassesAsync(null)).Value.Rows.Single();
        row.ClassCode.ShouldBe("F & F");
        row.ReportingCategory.ShouldBe("Furniture & Fixtures");
        row.IsDepreciable.ShouldBeTrue();
        row.IsAuc.ShouldBeFalse();
    }

    [Fact]
    public async Task Several_classes_can_report_under_one_category()
    {
        // Five of the thirteen live classes roll up to Plant & Machinery. That
        // is the whole reason ReportingCategory is a column and not a table.
        await fixture.ResetAsync();
        await CreateClassAsync("Ins eqpt", "Installation eqpt", "Plant & Machinery");
        await CreateClassAsync("Fty eqpt", "Factory eqpt", "Plant & Machinery");
        await CreateClassAsync("P&M", "Plant.& Machinery", "Plant & Machinery");

        var rows = (await SearchClassesAsync(null)).Value.Rows;
        rows.Count.ShouldBe(3);
        rows.ShouldAllBe(r => r.ReportingCategory == "Plant & Machinery");
    }

    [Fact]
    public async Task Land_can_be_marked_as_not_depreciating()
    {
        await fixture.ResetAsync();

        await CreateClassAsync("LH Land", "Lease Hold  land", "Leasehold Land", isDepreciable: false);

        (await SearchClassesAsync(null)).Value.Rows.Single().IsDepreciable.ShouldBeFalse();
    }

    [Fact]
    public async Task A_class_can_be_edited_and_retired()
    {
        await fixture.ResetAsync();
        var created = await CreateClassAsync("OLD", "Old Name", "Office Equipments");

        var updated = await UpdateClassAsync(
            created.Value.Id, "NEW", "New Name", "Computers", isActive: false);

        updated.IsSuccess.ShouldBeTrue();
        var row = (await SearchClassesAsync(null)).Value.Rows.Single();
        row.ClassCode.ShouldBe("NEW");
        row.ClassName.ShouldBe("New Name");
        row.ReportingCategory.ShouldBe("Computers");
        row.IsActive.ShouldBeFalse();
    }

    // ---------------------------------------------------- classes: negative

    [Fact]
    public async Task Two_classes_cannot_share_a_code()
    {
        await fixture.ResetAsync();
        await CreateClassAsync("AUC", "AUC", "AUC");

        var result = await CreateClassAsync("AUC", "Something else", "AUC");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("AssetClass.CodeTaken");
    }

    [Fact]
    public async Task Two_classes_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        await CreateClassAsync("A", "Vehicles", "Vehicles");

        var result = await CreateClassAsync("B", "Vehicles", "Vehicles");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("AssetClass.NameTaken");
    }

    [Fact]
    public async Task An_unknown_class_cannot_be_updated()
    {
        await fixture.ResetAsync();

        (await UpdateClassAsync(987654, "X", "Ghost", "None")).Error!.Code
            .ShouldBe("AssetClass.NotFound");
    }

    // -------------------------------------------------------- classes: edge

    [Fact]
    public async Task Creating_a_class_never_makes_a_second_AUC()
    {
        // IsAuc is absent from the command on purpose: the capitalisation step
        // finds its source class by that flag, and two would make it ambiguous.
        await fixture.ResetAsync();
        await CreateClassAsync("AUC", "AUC", "AUC");
        await CreateClassAsync("AUC2", "Also under construction", "AUC");

        (await SearchClassesAsync(null)).Value.Rows.Count(r => r.IsAuc).ShouldBe(0);
    }

    [Fact]
    public async Task The_AUC_class_cannot_be_retired()
    {
        await fixture.ResetAsync();
        var aucId = await SeedAucClassAsync();

        var result = await UpdateClassAsync(aucId, "AUC", "AUC", "AUC", isActive: false);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("AssetClass.AucMustStayActive");
    }

    [Fact]
    public async Task The_AUC_class_can_still_be_edited_while_it_stays_active()
    {
        await fixture.ResetAsync();
        var aucId = await SeedAucClassAsync();

        var result = await UpdateClassAsync(
            aucId, "AUC", "Assets Under Construction", "AUC", isActive: true);

        result.IsSuccess.ShouldBeTrue();
        (await SearchClassesAsync(null)).Value.Rows
            .Single(r => r.IsAuc).ClassName.ShouldBe("Assets Under Construction");
    }

    // ------------------------------------------------- chart of accounts

    [Fact]
    public async Task A_code_can_be_created_listed_and_edited()
    {
        await fixture.ResetAsync();

        var created = await CreateAccountAsync("1200100", "Furniture - Gross Block");
        created.IsSuccess.ShouldBeTrue();

        var updated = await UpdateAccountAsync(
            created.Value.Id, "1200100", "Furniture and Fixtures - Gross Block", isActive: false);

        updated.IsSuccess.ShouldBeTrue();
        var row = (await SearchAccountsAsync(null)).Value.Rows.Single();
        row.Description.ShouldBe("Furniture and Fixtures - Gross Block");
        row.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task A_code_may_have_no_description()
    {
        await fixture.ResetAsync();

        var created = await CreateAccountAsync("9999999", null);

        created.IsSuccess.ShouldBeTrue();
        (await SearchAccountsAsync(null)).Value.Rows.Single().Description.ShouldBeNull();
    }

    [Fact]
    public async Task A_whitespace_description_is_stored_as_no_description()
    {
        await fixture.ResetAsync();

        await CreateAccountAsync("8888888", "   ");

        (await SearchAccountsAsync(null)).Value.Rows.Single().Description.ShouldBeNull();
    }

    [Fact]
    public async Task Two_codes_cannot_share_a_value()
    {
        await fixture.ResetAsync();
        await CreateAccountAsync("1200100", "First");

        var result = await CreateAccountAsync("1200100", "Second");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("ChartOfAccount.CodeTaken");
    }

    [Fact]
    public async Task An_unknown_code_cannot_be_updated()
    {
        await fixture.ResetAsync();

        (await UpdateAccountAsync(987654, "X", null)).Error!.Code.ShouldBe("ChartOfAccount.NotFound");
    }

    [Fact]
    public async Task Codes_come_back_in_code_order()
    {
        await fixture.ResetAsync();
        await CreateAccountAsync("3000", null);
        await CreateAccountAsync("1000", null);
        await CreateAccountAsync("2000", null);

        (await SearchAccountsAsync(null)).Value.Rows
            .Select(r => r.CoaCode)
            .ShouldBe(["1000", "2000", "3000"]);
    }

    // -------------------------------------------------------------- helpers

    /// <summary>
    /// The AUC class, inserted directly. No slice can create one — that is the
    /// point of <c>Creating_a_class_never_makes_a_second_AUC</c> — so the tests
    /// that need one seed it the way the design script does.
    /// </summary>
    private async Task<int> SeedAucClassAsync()
    {
        await using var context = fixture.NewAssetsContext();
        var auc = new AssetClass
        {
            ClassCode = "AUC",
            ClassName = "AUC",
            ReportingCategory = "AUC",
            IsDepreciable = false,
            IsIntangible = false,
            IsAuc = true,
            IsActive = true,
            CreatedOnUtc = fixture.Clock.UtcNow,
            CreatedBy = "test",
        };
        context.AssetClasses.Add(auc);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return auc.Id;
    }

    private Task<Result<CreateAssetClassResponse>> CreateClassAsync(
        string code, string name, string category, bool isDepreciable = true, bool isIntangible = false)
    {
        var handler = new CreateAssetClassHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new CreateAssetClassCommand(code, name, category, isDepreciable, isIntangible),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<UpdateAssetClassResponse>> UpdateClassAsync(
        int id, string code, string name, string category,
        bool isDepreciable = true, bool isIntangible = false, bool isActive = true)
    {
        var handler = new UpdateAssetClassHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new UpdateAssetClassCommand(id, code, name, category, isDepreciable, isIntangible, isActive),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchAssetClassesResponse>> SearchClassesAsync(bool? isActive)
    {
        var handler = new SearchAssetClassesHandler(fixture.NewAssetsContext());
        return handler.HandleAsync(
            new SearchAssetClassesQuery(isActive), TestContext.Current.CancellationToken);
    }

    private Task<Result<CreateChartOfAccountResponse>> CreateAccountAsync(
        string code, string? description)
    {
        var handler = new CreateChartOfAccountHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new CreateChartOfAccountCommand(
                code, string.IsNullOrWhiteSpace(description) ? null : description.Trim()),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<UpdateChartOfAccountResponse>> UpdateAccountAsync(
        int id, string code, string? description, bool isActive = true)
    {
        var handler = new UpdateChartOfAccountHandler(
            fixture.NewAssetsContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new UpdateChartOfAccountCommand(
                id, code, string.IsNullOrWhiteSpace(description) ? null : description.Trim(), isActive),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchChartOfAccountsResponse>> SearchAccountsAsync(bool? isActive)
    {
        var handler = new SearchChartOfAccountsHandler(fixture.NewAssetsContext());
        return handler.HandleAsync(
            new SearchChartOfAccountsQuery(isActive), TestContext.Current.CancellationToken);
    }
}
