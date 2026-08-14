using AMS.Modules.Organization.Features.CreateApplication;
using AMS.Modules.Organization.Features.CreateEmployee;
using AMS.Modules.Organization.Features.GetEmployeeApplications;
using AMS.Modules.Organization.Features.GetMyApplicationAccess;
using AMS.Modules.Organization.Features.GrantApplicationAccess;
using AMS.Modules.Organization.Features.RevokeApplicationAccess;
using AMS.Modules.Organization.Features.SearchApplications;
using AMS.Modules.Organization.Features.UpdateApplication;
using AMS.SharedKernel.Results;

namespace AMS.Modules.Organization.Tests;

/// <summary>
/// Catalogue screen: Applications and Access. Features: Application master,
/// Grant and revoke application access, See my application access.
/// </summary>
[Collection(nameof(OrganizationCollectionDefinition))]
public sealed class ApplicationsAndAccessTests(OrganizationFixture fixture)
{
    // -------------------------------------------------- application master

    [Fact]
    public async Task An_application_can_be_created_and_listed()
    {
        await fixture.ResetAsync();

        var created = await CreateApplicationAsync("SAP");

        created.IsSuccess.ShouldBeTrue();
        (await SearchApplicationsAsync()).Value.Rows.Single().ApplicationName.ShouldBe("SAP");
    }

    [Fact]
    public async Task Two_applications_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        await CreateApplicationAsync("SAP");

        (await CreateApplicationAsync("SAP")).Error!.Code.ShouldBe("Application.NameTaken");
    }

    [Fact]
    public async Task An_application_can_be_renamed_and_retired()
    {
        await fixture.ResetAsync();
        var created = await CreateApplicationAsync("SAP");

        var result = await UpdateApplicationAsync(created.Value.Id, "SAP S/4HANA", isActive: false);

        result.Value.ApplicationName.ShouldBe("SAP S/4HANA");
        result.Value.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task An_unknown_application_cannot_be_updated()
    {
        await fixture.ResetAsync();

        (await UpdateApplicationAsync(8888, "Ghost", true)).Error!.Code.ShouldBe("Application.NotFound");
    }

    [Fact]
    public async Task The_application_list_counts_only_current_holders()
    {
        await fixture.ResetAsync();
        var (employee, application) = await SeedPairAsync();
        var other = await CreateEmployeeAsync("E-0002", "Other");

        await GrantAsync(employee, application);
        await GrantAsync(other, application);
        await RevokeAsync(other, application);

        // A revoked grant is history, not access.
        (await SearchApplicationsAsync()).Value.Rows.Single().ActiveGrantCount.ShouldBe(1);
    }

    // ------------------------------------------------------ grant and revoke

    [Fact]
    public async Task Access_can_be_granted_and_read_back()
    {
        await fixture.ResetAsync();
        var (employee, application) = await SeedPairAsync();

        var result = await GrantAsync(employee, application, login: "arao");

        result.IsSuccess.ShouldBeTrue();
        var rows = (await GetEmployeeApplicationsAsync(employee)).Value.Rows;
        rows.Single().ApplicationName.ShouldBe("SAP");
        rows.Single().ApplicationLoginId.ShouldBe("arao");
        rows.Single().RevokedOnUtc.ShouldBeNull();
    }

    [Fact]
    public async Task The_same_access_cannot_be_granted_twice_while_it_is_held()
    {
        // UX_EmployeeApplication_OneActive, filtered on RevokedOnUtc IS NULL.
        await fixture.ResetAsync();
        var (employee, application) = await SeedPairAsync();
        await GrantAsync(employee, application);

        var result = await GrantAsync(employee, application);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("ApplicationAccess.AlreadyGranted");
        result.Error.Kind.ShouldBe(ErrorKind.Conflict);
    }

    [Fact]
    public async Task Access_can_be_granted_again_after_it_was_revoked()
    {
        // The other half of the filter, and the reason it is filtered at all:
        // people rejoin teams.
        await fixture.ResetAsync();
        var (employee, application) = await SeedPairAsync();
        await GrantAsync(employee, application);
        await RevokeAsync(employee, application);

        var regranted = await GrantAsync(employee, application);

        regranted.IsSuccess.ShouldBeTrue();

        var all = (await GetEmployeeApplicationsAsync(employee, includeRevoked: true)).Value.Rows;
        all.Count.ShouldBe(2, "the revoked row stays as the record that access WAS held");
        all.Count(r => r.RevokedOnUtc is null).ShouldBe(1);
    }

    [Fact]
    public async Task Revoking_stamps_the_row_instead_of_deleting_it()
    {
        await fixture.ResetAsync();
        var (employee, application) = await SeedPairAsync();
        await GrantAsync(employee, application);

        var result = await RevokeAsync(employee, application);

        result.IsSuccess.ShouldBeTrue();
        (await fixture.ScalarAsync<int>(
            $"SELECT COUNT(*) FROM [Organization].[EmployeeApplication] WHERE [EmployeeId] = {employee};"))
            .ShouldBe(1, "an audit asks what access somebody HAD after they leave");
    }

    [Fact]
    public async Task Revoking_access_nobody_holds_is_refused()
    {
        await fixture.ResetAsync();
        var (employee, application) = await SeedPairAsync();

        (await RevokeAsync(employee, application)).Error!.Code.ShouldBe("ApplicationAccess.NotFound");
    }

    [Fact]
    public async Task Revoking_twice_is_refused_the_second_time()
    {
        await fixture.ResetAsync();
        var (employee, application) = await SeedPairAsync();
        await GrantAsync(employee, application);
        await RevokeAsync(employee, application);

        (await RevokeAsync(employee, application)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Granting_to_an_unknown_employee_or_application_is_refused()
    {
        await fixture.ResetAsync();
        var (employee, application) = await SeedPairAsync();

        (await GrantAsync(9999, application)).Error!.Code.ShouldBe("Employee.NotFound");
        (await GrantAsync(employee, 9999)).Error!.Code.ShouldBe("Application.NotFound");
    }

    [Fact]
    public async Task Two_employees_can_hold_the_same_application()
    {
        // The unique index is on the PAIR, not on either column.
        await fixture.ResetAsync();
        var (first, application) = await SeedPairAsync();
        var second = await CreateEmployeeAsync("E-0002", "Second");

        (await GrantAsync(first, application)).IsSuccess.ShouldBeTrue();
        (await GrantAsync(second, application)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task One_employee_can_hold_several_applications()
    {
        await fixture.ResetAsync();
        var (employee, sap) = await SeedPairAsync();
        var dms = await CreateApplicationAsync("DMS");

        await GrantAsync(employee, sap);
        await GrantAsync(employee, dms.Value.Id);

        (await GetEmployeeApplicationsAsync(employee)).Value.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Listing_access_for_an_unknown_employee_is_refused()
    {
        await fixture.ResetAsync();

        (await GetEmployeeApplicationsAsync(7777)).Error!.Code.ShouldBe("Employee.NotFound");
    }

    [Fact]
    public async Task Revoked_grants_are_hidden_unless_asked_for()
    {
        await fixture.ResetAsync();
        var (employee, application) = await SeedPairAsync();
        await GrantAsync(employee, application);
        await RevokeAsync(employee, application);

        (await GetEmployeeApplicationsAsync(employee)).Value.Rows.ShouldBeEmpty();
        (await GetEmployeeApplicationsAsync(employee, includeRevoked: true)).Value.Rows.Count.ShouldBe(1);
    }

    // --------------------------------------------- see my application access

    [Fact]
    public async Task An_employee_sees_their_own_current_access()
    {
        await fixture.ResetAsync();
        var (employee, application) = await SeedPairAsync();
        await GrantAsync(employee, application, login: "arao");

        var result = await GetMyAccessAsync(employee);

        result.Value.EmployeeId.ShouldBe(employee);
        result.Value.Rows.Single().ApplicationName.ShouldBe("SAP");
        result.Value.Rows.Single().ApplicationLoginId.ShouldBe("arao");
    }

    [Fact]
    public async Task An_employee_does_not_see_access_that_was_withdrawn()
    {
        // Read-only, current-only. Showing withdrawals invites a conversation
        // this screen cannot have.
        await fixture.ResetAsync();
        var (employee, application) = await SeedPairAsync();
        await GrantAsync(employee, application);
        await RevokeAsync(employee, application);

        (await GetMyAccessAsync(employee)).Value.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_login_with_no_employee_record_gets_a_null_id_not_an_empty_list()
    {
        // A service account, or an administrator not in the directory. The
        // screen must be able to say WHY it is empty.
        await fixture.ResetAsync();

        var result = await GetMyAccessAsync(null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.EmployeeId.ShouldBeNull();
        result.Value.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task One_employee_cannot_see_another_employees_access_through_this_screen()
    {
        await fixture.ResetAsync();
        var (mine, application) = await SeedPairAsync();
        var theirs = await CreateEmployeeAsync("E-0002", "Somebody Else");
        await GrantAsync(theirs, application);

        // The query takes the employee id from the caller's claims, never from
        // the request, so there is nothing to tamper with.
        (await GetMyAccessAsync(mine)).Value.Rows.ShouldBeEmpty();
    }

    // ------------------------------------------------------------ helpers

    private async Task<(int EmployeeId, int ApplicationId)> SeedPairAsync()
    {
        var employee = await CreateEmployeeAsync("E-0001", "Asha Rao");
        var application = await CreateApplicationAsync("SAP");
        return (employee, application.Value.Id);
    }

    private async Task<int> CreateEmployeeAsync(string code, string name)
    {
        await using var context = fixture.NewContext();
        var result = await new CreateEmployeeHandler(
                context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(
                new CreateEmployeeCommand(code, name, null, null, null, null, null),
                TestContext.Current.CancellationToken);
        return result.Value.Id;
    }

    private async Task<Result<CreateApplicationResponse>> CreateApplicationAsync(string name)
    {
        await using var context = fixture.NewContext();
        return await new CreateApplicationHandler(
                context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(new CreateApplicationCommand(name), TestContext.Current.CancellationToken);
    }

    private async Task<Result<UpdateApplicationResponse>> UpdateApplicationAsync(
        int id, string name, bool isActive)
    {
        await using var context = fixture.NewContext();
        return await new UpdateApplicationHandler(
                context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(
                new UpdateApplicationCommand(id, name, isActive), TestContext.Current.CancellationToken);
    }

    private async Task<Result<SearchApplicationsResponse>> SearchApplicationsAsync()
    {
        await using var context = fixture.NewContext();
        return await new SearchApplicationsHandler(context)
            .HandleAsync(new SearchApplicationsQuery(null, null), TestContext.Current.CancellationToken);
    }

    private async Task<Result<GrantApplicationAccessResponse>> GrantAsync(
        int employeeId, int applicationId, string? login = null)
    {
        await using var context = fixture.NewContext();
        return await new GrantApplicationAccessHandler(
                context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(
                new GrantApplicationAccessCommand(employeeId, applicationId, login),
                TestContext.Current.CancellationToken);
    }

    private async Task<Result<RevokeApplicationAccessResponse>> RevokeAsync(int employeeId, int applicationId)
    {
        await using var context = fixture.NewContext();
        return await new RevokeApplicationAccessHandler(context, fixture.Clock, fixture.CurrentUser)
            .HandleAsync(
                new RevokeApplicationAccessCommand(employeeId, applicationId),
                TestContext.Current.CancellationToken);
    }

    private async Task<Result<GetEmployeeApplicationsResponse>> GetEmployeeApplicationsAsync(
        int employeeId, bool includeRevoked = false)
    {
        await using var context = fixture.NewContext();
        return await new GetEmployeeApplicationsHandler(context)
            .HandleAsync(
                new GetEmployeeApplicationsQuery(employeeId, includeRevoked),
                TestContext.Current.CancellationToken);
    }

    private async Task<Result<GetMyApplicationAccessResponse>> GetMyAccessAsync(int? employeeId)
    {
        await using var context = fixture.NewContext();
        return await new GetMyApplicationAccessHandler(context)
            .HandleAsync(new GetMyApplicationAccessQuery(employeeId), TestContext.Current.CancellationToken);
    }
}
