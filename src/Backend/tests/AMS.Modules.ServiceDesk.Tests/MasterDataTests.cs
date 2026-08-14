using AMS.Modules.ServiceDesk.Features.CreateRequestCategory;
using AMS.Modules.ServiceDesk.Features.CreateRequestSubCategory;
using AMS.Modules.ServiceDesk.Features.CreateServiceTemplate;
using AMS.Modules.ServiceDesk.Features.CreateSupportTeam;
using AMS.Modules.ServiceDesk.Features.SearchRequestCategories;
using AMS.Modules.ServiceDesk.Features.SearchServiceTemplates;
using AMS.Modules.ServiceDesk.Features.SearchSupportTeams;
using AMS.Modules.ServiceDesk.Features.SetSupportTeamMembers;
using AMS.Modules.ServiceDesk.Features.UpdateRequestCategory;
using AMS.Modules.ServiceDesk.Features.UpdateRequestSubCategory;
using AMS.Modules.ServiceDesk.Features.UpdateServiceTemplate;
using AMS.Modules.ServiceDesk.Features.UpdateSupportTeam;
using AMS.Modules.ServiceDesk.Domain;
using AMS.SharedKernel.Results;

namespace AMS.Modules.ServiceDesk.Tests;

/// <summary>
/// Catalogue screens: Categories, Support Teams, Service Templates — the master
/// data a ticket refers to. Pass one of three.
/// </summary>
[Collection(nameof(ServiceDeskCollectionDefinition))]
public sealed class MasterDataTests(ServiceDeskFixture fixture)
{
    // ------------------------------------------------------------ categories

    [Fact]
    public async Task A_category_can_be_created_and_listed()
    {
        await fixture.ResetAsync();

        var created = await CreateCategoryAsync("Desktop Support");

        created.IsSuccess.ShouldBeTrue();
        (await SearchCategoriesAsync()).Value.Rows.Single().CategoryName.ShouldBe("Desktop Support");
    }

    [Fact]
    public async Task A_category_comes_back_with_its_sub_categories()
    {
        // The screen is a tree and cannot draw the top without the bottom.
        await fixture.ResetAsync();
        var category = await CreateCategoryAsync("Desktop Support");
        await CreateSubCategoryAsync(category.Value.Id, "Printer");
        await CreateSubCategoryAsync(category.Value.Id, "Laptop");

        var row = (await SearchCategoriesAsync()).Value.Rows.Single();
        row.SubCategories.Select(s => s.SubCategoryName).ShouldBe(["Laptop", "Printer"]);
    }

    [Fact]
    public async Task The_same_sub_category_name_may_sit_under_two_categories()
    {
        // UX_RequestSubCategory_Name is on (CategoryId, Name), not Name alone:
        // "Hardware" is reasonable under both Desktop Support and Facilities.
        await fixture.ResetAsync();
        var first = await CreateCategoryAsync("Desktop Support");
        var second = await CreateCategoryAsync("Facilities");
        await CreateSubCategoryAsync(first.Value.Id, "Hardware");

        var result = await CreateSubCategoryAsync(second.Value.Id, "Hardware");

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Two_categories_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        await CreateCategoryAsync("Network");

        var result = await CreateCategoryAsync("Network");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("RequestCategory.NameTaken");
    }

    [Fact]
    public async Task Two_sub_categories_under_one_category_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        var category = await CreateCategoryAsync("Network");
        await CreateSubCategoryAsync(category.Value.Id, "VPN");

        var result = await CreateSubCategoryAsync(category.Value.Id, "VPN");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("RequestSubCategory.NameTaken");
    }

    [Fact]
    public async Task A_sub_category_cannot_hang_off_a_category_that_does_not_exist()
    {
        await fixture.ResetAsync();

        (await CreateSubCategoryAsync(987654, "Orphan")).Error!.Code
            .ShouldBe("RequestCategory.NotFound");
    }

    [Fact]
    public async Task Retiring_a_category_leaves_its_sub_categories_alone()
    {
        // Cascading the flag would make reactivating the parent silently
        // resurrect sub-categories somebody retired on purpose.
        await fixture.ResetAsync();
        var category = await CreateCategoryAsync("Legacy");
        await CreateSubCategoryAsync(category.Value.Id, "Still here");

        await UpdateCategoryAsync(category.Value.Id, "Legacy", isActive: false);

        var row = (await SearchCategoriesAsync()).Value.Rows.Single();
        row.IsActive.ShouldBeFalse();
        row.SubCategories.Single().IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task An_unknown_category_or_sub_category_cannot_be_updated()
    {
        await fixture.ResetAsync();

        (await UpdateCategoryAsync(987654, "Ghost")).Error!.Code
            .ShouldBe("RequestCategory.NotFound");
        (await UpdateSubCategoryAsync(987654, "Ghost")).Error!.Code
            .ShouldBe("RequestSubCategory.NotFound");
    }

    // --------------------------------------------------------- support teams

    [Fact]
    public async Task A_team_can_be_created_and_listed()
    {
        await fixture.ResetAsync();

        var created = await CreateTeamAsync("South Desk", regionId: 3);

        created.IsSuccess.ShouldBeTrue();
        var row = (await SearchTeamsAsync()).Value.Rows.Single();
        row.TeamName.ShouldBe("South Desk");
        row.RegionId.ShouldBe(3);
        row.MemberCount.ShouldBe(0);
    }

    [Fact]
    public async Task Only_one_team_can_be_the_default()
    {
        // A filtered unique index. The second attempt collides rather than
        // silently demoting the team somebody else chose.
        await fixture.ResetAsync();
        await CreateTeamAsync("First", isDefault: true);

        var second = await CreateTeamAsync("Second", isDefault: true);

        second.IsSuccess.ShouldBeFalse();
        second.Error!.Code.ShouldBe("SupportTeam.DefaultExists");
    }

    [Fact]
    public async Task The_default_team_cannot_be_retired()
    {
        // It is where routing sends anything it cannot place. An inactive
        // default means tickets with nowhere to go and nobody watching them.
        await fixture.ResetAsync();
        var team = await CreateTeamAsync("Fallback", isDefault: true);

        var result = await UpdateTeamAsync(team.Value.Id, "Fallback", isDefault: true, isActive: false);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("SupportTeam.DefaultMustStayActive");
    }

    [Fact]
    public async Task Two_teams_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        await CreateTeamAsync("North Desk");

        (await CreateTeamAsync("North Desk")).Error!.Code.ShouldBe("SupportTeam.NameTaken");
    }

    [Fact]
    public async Task Members_and_leads_are_set_as_a_whole()
    {
        await fixture.ResetAsync();
        var team = await CreateTeamAsync("North Desk");

        var result = await SetMembersAsync(team.Value.Id, (10, true), (11, false), (12, false));

        result.IsSuccess.ShouldBeTrue();
        result.Value.MemberCount.ShouldBe(3);
        result.Value.LeadCount.ShouldBe(1);
        var row = (await SearchTeamsAsync()).Value.Rows.Single();
        row.MemberCount.ShouldBe(3);
        row.LeadUserIds.ShouldBe([10]);
    }

    [Fact]
    public async Task Setting_the_members_again_replaces_the_whole_set()
    {
        await fixture.ResetAsync();
        var team = await CreateTeamAsync("North Desk");
        await SetMembersAsync(team.Value.Id, (10, true), (11, false));

        await SetMembersAsync(team.Value.Id, (11, true), (12, false));

        var row = (await SearchTeamsAsync()).Value.Rows.Single();
        row.MemberCount.ShouldBe(2);
        row.LeadUserIds.ShouldBe([11]);
    }

    [Fact]
    public async Task A_team_with_members_needs_a_lead()
    {
        // Escalation has to reach somebody by name.
        await fixture.ResetAsync();
        var team = await CreateTeamAsync("North Desk");

        var result = await SetMembersAsync(team.Value.Id, (10, false), (11, false));

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("SupportTeam.NoLead");
    }

    [Fact]
    public async Task Emptying_a_team_is_allowed()
    {
        // The lead rule is about teams that HAVE members. A team being stood
        // down must not be blocked by a rule about escalation.
        await fixture.ResetAsync();
        var team = await CreateTeamAsync("North Desk");
        await SetMembersAsync(team.Value.Id, (10, true));

        var result = await SetMembersAsync(team.Value.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.MemberCount.ShouldBe(0);
    }

    [Fact]
    public async Task Somebody_listed_twice_is_taken_once_and_the_lead_flag_wins()
    {
        await fixture.ResetAsync();
        var team = await CreateTeamAsync("North Desk");

        var result = await SetMembersAsync(team.Value.Id, (10, false), (10, true));

        result.IsSuccess.ShouldBeTrue();
        result.Value.MemberCount.ShouldBe(1);
        result.Value.LeadCount.ShouldBe(1);
    }

    [Fact]
    public async Task Members_cannot_be_set_on_a_team_that_does_not_exist()
    {
        await fixture.ResetAsync();

        (await SetMembersAsync(987654, (10, true))).Error!.Code.ShouldBe("SupportTeam.NotFound");
    }

    // ------------------------------------------------------ service templates

    [Fact]
    public async Task A_template_can_be_created_and_listed_in_display_order()
    {
        await fixture.ResetAsync();
        await CreateTemplateAsync("Second", displayOrder: 2);
        await CreateTemplateAsync("First", displayOrder: 1);

        (await SearchTemplatesAsync()).Value.Rows
            .Select(t => t.TemplateName).ShouldBe(["First", "Second"]);
    }

    [Fact]
    public async Task A_template_can_pre_fill_a_category_and_sub_category()
    {
        await fixture.ResetAsync();
        var category = await CreateCategoryAsync("Desktop Support");
        var sub = await CreateSubCategoryAsync(category.Value.Id, "Laptop");

        var created = await CreateTemplateAsync(
            "New joiner laptop", categoryId: category.Value.Id, subCategoryId: sub.Value.Id);

        created.IsSuccess.ShouldBeTrue();
        var row = (await SearchTemplatesAsync()).Value.Rows.Single();
        row.RequestCategoryId.ShouldBe(category.Value.Id);
        row.RequestSubCategoryId.ShouldBe(sub.Value.Id);
    }

    [Fact]
    public async Task A_sub_category_from_a_different_category_is_refused()
    {
        // Nothing in the schema forbids it — the two columns are independent
        // FKs — and the result would be a ticket classified two ways at once.
        await fixture.ResetAsync();
        var first = await CreateCategoryAsync("Desktop Support");
        var second = await CreateCategoryAsync("Facilities");
        var sub = await CreateSubCategoryAsync(second.Value.Id, "Chairs");

        var result = await CreateTemplateAsync(
            "Wrong pairing", categoryId: first.Value.Id, subCategoryId: sub.Value.Id);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("ServiceTemplate.SubCategoryMismatch");
    }

    [Fact]
    public async Task A_template_cannot_point_at_lookups_that_do_not_exist()
    {
        await fixture.ResetAsync();

        (await CreateTemplateAsync("A", categoryId: 987654)).Error!.Code
            .ShouldBe("RequestCategory.NotFound");
        (await CreateTemplateAsync("B", teamId: 987654)).Error!.Code
            .ShouldBe("SupportTeam.NotFound");
        (await CreateTemplateAsync("C", subCategoryId: 987654)).Error!.Code
            .ShouldBe("RequestSubCategory.NotFound");
    }

    [Fact]
    public async Task Two_templates_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        await CreateTemplateAsync("New joiner");

        (await CreateTemplateAsync("New joiner")).Error!.Code
            .ShouldBe("ServiceTemplate.NameTaken");
    }

    [Fact]
    public async Task A_template_can_be_edited_and_retired()
    {
        await fixture.ResetAsync();
        var created = await CreateTemplateAsync("Old name");

        var updated = await UpdateTemplateAsync(created.Value.Id, "New name", isActive: false);

        updated.IsSuccess.ShouldBeTrue();
        var row = (await SearchTemplatesAsync()).Value.Rows.Single();
        row.TemplateName.ShouldBe("New name");
        row.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task The_request_kind_survives_an_edit()
    {
        // It decides which screen the template appears on and whether an
        // approval workflow applies, so it is not editable.
        await fixture.ResetAsync();
        var created = await CreateTemplateAsync("Joiner", requestKind: RequestKind.NewService);

        await UpdateTemplateAsync(created.Value.Id, "Joiner");

        (await SearchTemplatesAsync()).Value.Rows.Single().RequestKind.ShouldBe(RequestKind.NewService);
    }

    [Fact]
    public async Task An_unknown_template_or_team_cannot_be_updated()
    {
        await fixture.ResetAsync();

        (await UpdateTemplateAsync(987654, "Ghost")).Error!.Code
            .ShouldBe("ServiceTemplate.NotFound");
        (await UpdateTeamAsync(987654, "Ghost")).Error!.Code
            .ShouldBe("SupportTeam.NotFound");
    }

    [Fact]
    public async Task Empty_lists_are_empty_lists_and_not_failures()
    {
        await fixture.ResetAsync();

        (await SearchCategoriesAsync()).Value.Rows.ShouldBeEmpty();
        (await SearchTeamsAsync()).Value.Rows.ShouldBeEmpty();
        (await SearchTemplatesAsync()).Value.Rows.ShouldBeEmpty();
    }

    // -------------------------------------------------------------- helpers

    private Task<Result<CreateRequestCategoryResponse>> CreateCategoryAsync(string name)
    {
        var handler = new CreateRequestCategoryHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new CreateRequestCategoryCommand(name), TestContext.Current.CancellationToken);
    }

    private Task<Result<UpdateRequestCategoryResponse>> UpdateCategoryAsync(
        int id, string name, bool isActive = true)
    {
        var handler = new UpdateRequestCategoryHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new UpdateRequestCategoryCommand(id, name, isActive),
            TestContext.Current.CancellationToken);
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

    private Task<Result<UpdateRequestSubCategoryResponse>> UpdateSubCategoryAsync(
        int id, string name, bool isActive = true)
    {
        var handler = new UpdateRequestSubCategoryHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new UpdateRequestSubCategoryCommand(id, name, isActive),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchRequestCategoriesResponse>> SearchCategoriesAsync()
    {
        var handler = new SearchRequestCategoriesHandler(fixture.NewContext());
        return handler.HandleAsync(
            new SearchRequestCategoriesQuery(null), TestContext.Current.CancellationToken);
    }

    private Task<Result<CreateSupportTeamResponse>> CreateTeamAsync(
        string name, int? regionId = null, bool isDefault = false)
    {
        var handler = new CreateSupportTeamHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new CreateSupportTeamCommand(name, regionId, null, isDefault),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<UpdateSupportTeamResponse>> UpdateTeamAsync(
        int id, string name, bool isDefault = false, bool isActive = true)
    {
        var handler = new UpdateSupportTeamHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new UpdateSupportTeamCommand(id, name, null, null, isDefault, isActive),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SetSupportTeamMembersResponse>> SetMembersAsync(
        int teamId, params (int UserId, bool IsLead)[] members)
    {
        var handler = new SetSupportTeamMembersHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new SetSupportTeamMembersCommand(
                teamId,
                [.. members.Select(m => new SetSupportTeamMembersCommand.Member(m.UserId, m.IsLead))]),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchSupportTeamsResponse>> SearchTeamsAsync()
    {
        var handler = new SearchSupportTeamsHandler(fixture.NewContext());
        return handler.HandleAsync(
            new SearchSupportTeamsQuery(null, null), TestContext.Current.CancellationToken);
    }

    private Task<Result<CreateServiceTemplateResponse>> CreateTemplateAsync(
        string name,
        string requestKind = RequestKind.SupportTicket,
        int? categoryId = null,
        int? subCategoryId = null,
        int? teamId = null,
        int displayOrder = 0)
    {
        var handler = new CreateServiceTemplateHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new CreateServiceTemplateCommand(
                name, requestKind, categoryId, subCategoryId, "Medium", teamId,
                "A subject", null, false, displayOrder),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<UpdateServiceTemplateResponse>> UpdateTemplateAsync(
        int id, string name, bool isActive = true)
    {
        var handler = new UpdateServiceTemplateHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);
        return handler.HandleAsync(
            new UpdateServiceTemplateCommand(
                id, name, null, null, "Medium", null, "A subject", null, false, 0, isActive),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchServiceTemplatesResponse>> SearchTemplatesAsync()
    {
        var handler = new SearchServiceTemplatesHandler(fixture.NewContext());
        return handler.HandleAsync(
            new SearchServiceTemplatesQuery(null, null), TestContext.Current.CancellationToken);
    }
}
