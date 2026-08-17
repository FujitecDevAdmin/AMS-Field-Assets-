using AMS.Modules.Organization.Features.CreateEmployee;
using AMS.Modules.Organization.Features.DeactivateEmployee;
using AMS.Modules.Organization.Features.GetEmployee;
using AMS.Modules.Organization.Features.SearchEmployees;
using AMS.Modules.Organization.Features.UpdateEmployee;
using AMS.SharedKernel.Results;

namespace AMS.Modules.Organization.Tests;

/// <summary>
/// Catalogue screen: Employee Directory. Features: Employee directory,
/// Reporting manager, Deactivate a leaver.
/// </summary>
/// <remarks>
/// Employee is the first system-versioned table the application writes, so
/// these are also the first production tests of R2-22's ConcurrencyStamp.
/// </remarks>
[Collection(nameof(OrganizationCollectionDefinition))]
public sealed class EmployeeDirectoryTests(OrganizationFixture fixture)
{
    // ------------------------------------------------------------ positive

    [Fact]
    public async Task An_employee_can_be_created_and_read_back()
    {
        await fixture.ResetAsync();

        var created = await CreateAsync("E-1001", "Asha Rao");

        created.IsSuccess.ShouldBeTrue();
        var fetched = await GetAsync(created.Value.Id);
        fetched.Value.FullName.ShouldBe("Asha Rao");
        fetched.Value.EmployeeCode.ShouldBe("E-1001");
        fetched.Value.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task The_directory_shows_department_branch_and_manager_names()
    {
        await fixture.ResetAsync();
        var (departmentId, branchId) = await SeedMasterDataAsync();
        var manager = await CreateAsync("E-0001", "Manager Person");

        await CreateAsync("E-1002", "Reportee", departmentId, branchId, manager.Value.Id);

        var row = (await SearchAsync(search: "Reportee")).Value.Rows.Single();
        row.DepartmentName.ShouldBe("IT");
        row.BranchName.ShouldBe("Bangalore");
        row.ReportingManagerName.ShouldBe("Manager Person");
    }

    [Fact]
    public async Task The_directory_pages_and_reports_the_total()
    {
        await fixture.ResetAsync();
        for (var i = 0; i < 6; i++)
        {
            await CreateAsync($"E-20{i:D2}", $"Person {i}");
        }

        var page = await SearchAsync(skip: 0, take: 4);

        page.Value.Rows.Count.ShouldBe(4);
        page.Value.TotalCount.ShouldBe(6);
    }

    [Fact]
    public async Task The_directory_filters_by_department_branch_and_active_flag()
    {
        await fixture.ResetAsync();
        var (departmentId, branchId) = await SeedMasterDataAsync();
        await CreateAsync("E-3001", "In IT", departmentId, branchId);
        var other = await CreateAsync("E-3002", "Elsewhere");

        (await SearchAsync(departmentId: departmentId)).Value.TotalCount.ShouldBe(1);
        (await SearchAsync(branchId: branchId)).Value.TotalCount.ShouldBe(1);

        await DeactivateAsync(other.Value.Id, other.Value.ETag);
        (await SearchAsync(isActive: false)).Value.TotalCount.ShouldBe(1);
        (await SearchAsync(isActive: true)).Value.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_employee_can_report_to_another_employee()
    {
        // Catalogue: "Each employee may report to another employee."
        await fixture.ResetAsync();
        var manager = await CreateAsync("E-0001", "Manager");
        var reportee = await CreateAsync("E-0002", "Reportee");

        var result = await UpdateAsync(
            reportee.Value.Id, "E-0002", "Reportee", reportee.Value.ETag, managerId: manager.Value.Id);

        result.IsSuccess.ShouldBeTrue();
        (await GetAsync(reportee.Value.Id)).Value.ReportingManagerName.ShouldBe("Manager");
    }

    // ------------------------------------------------------------ negative

    [Fact]
    public async Task Two_employees_cannot_share_a_code()
    {
        await fixture.ResetAsync();
        await CreateAsync("E-1001", "First");

        var result = await CreateAsync("E-1001", "Second");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Employee.CodeTaken");
        result.Error.Kind.ShouldBe(ErrorKind.Conflict);
    }

    [Fact]
    public async Task An_employee_code_is_upper_cased_so_case_cannot_duplicate_it()
    {
        await fixture.ResetAsync();
        await CreateAsync("e-1001", "First");

        (await CreateAsync("E-1001", "Second")).Error!.Code.ShouldBe("Employee.CodeTaken");
    }

    [Fact]
    public async Task Reporting_to_an_unknown_employee_is_refused_by_id()
    {
        await fixture.ResetAsync();

        var result = await CreateAsync("E-1001", "Orphan", managerId: 9999);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Employee.ManagerNotFound");
        result.Error.Message.ShouldContain("9999");
    }

    [Fact]
    public async Task An_unknown_employee_is_not_found()
    {
        await fixture.ResetAsync();

        (await GetAsync(4242)).Error!.Code.ShouldBe("Employee.NotFound");
    }

    // ---------------------------------------------------------------- edge

    [Fact]
    public async Task An_employee_cannot_report_to_themselves()
    {
        await fixture.ResetAsync();
        var employee = await CreateAsync("E-1001", "Lonely");

        var result = await UpdateAsync(
            employee.Value.Id, "E-1001", "Lonely", employee.Value.ETag, managerId: employee.Value.Id);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Employee.CannotReportToSelf");
    }

    [Fact]
    public async Task A_management_cycle_is_refused()
    {
        // A reports to B, then B is asked to report to A. The chain would never
        // terminate and "who approves this?" would have no answer.
        await fixture.ResetAsync();
        var a = await CreateAsync("E-0001", "A");
        var b = await CreateAsync("E-0002", "B");

        var linked = await UpdateAsync(a.Value.Id, "E-0001", "A", a.Value.ETag, managerId: b.Value.Id);
        linked.IsSuccess.ShouldBeTrue();

        var result = await UpdateAsync(b.Value.Id, "E-0002", "B", b.Value.ETag, managerId: a.Value.Id);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Employee.ManagementCycle");
    }

    [Fact]
    public async Task A_longer_management_cycle_is_also_refused()
    {
        await fixture.ResetAsync();
        var a = await CreateAsync("E-0001", "A");
        var b = await CreateAsync("E-0002", "B");
        var c = await CreateAsync("E-0003", "C");

        await UpdateAsync(a.Value.Id, "E-0001", "A", a.Value.ETag, managerId: b.Value.Id);
        var bAfter = await GetAsync(b.Value.Id);
        await UpdateAsync(b.Value.Id, "E-0002", "B", bAfter.Value.ETag, managerId: c.Value.Id);

        var cAfter = await GetAsync(c.Value.Id);
        var result = await UpdateAsync(c.Value.Id, "E-0003", "C", cAfter.Value.ETag, managerId: a.Value.Id);

        result.Error!.Code.ShouldBe("Employee.ManagementCycle");
    }

    // ------------------------------------------- R2-22 in production use

    [Fact]
    public async Task An_edit_with_the_current_stamp_succeeds_and_returns_a_new_one()
    {
        await fixture.ResetAsync();
        var created = await CreateAsync("E-1001", "Asha Rao");

        var result = await UpdateAsync(created.Value.Id, "E-1001", "Asha Rao-Menon", created.Value.ETag);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ETag.ShouldNotBe(created.Value.ETag, "the stamp must move on every write");
    }

    [Fact]
    public async Task A_stale_stamp_is_a_412_and_the_first_writer_survives()
    {
        // The exact scenario SysStartTime lost silently, with no delays at all.
        await fixture.ResetAsync();
        var created = await CreateAsync("E-1001", "Asha Rao");
        var stale = created.Value.ETag;

        await UpdateAsync(created.Value.Id, "E-1001", "First writer", stale);
        var second = await UpdateAsync(created.Value.Id, "E-1001", "Second writer", stale);

        second.IsSuccess.ShouldBeFalse();
        second.Error!.Kind.ShouldBe(ErrorKind.Concurrency);
        (await GetAsync(created.Value.Id)).Value.FullName.ShouldBe("First writer");
    }

    [Fact]
    public async Task A_malformed_stamp_is_a_validation_error_not_a_crash()
    {
        await fixture.ResetAsync();
        var created = await CreateAsync("E-1001", "Asha Rao");

        var result = await UpdateAsync(created.Value.Id, "E-1001", "Asha", "not-a-guid");

        result.Error!.Kind.ShouldBe(ErrorKind.Validation);
    }

    [Fact]
    public async Task Editing_an_employee_records_a_history_version()
    {
        // SysStartTime keeps its real job now that it is not the token.
        await fixture.ResetAsync();
        var created = await CreateAsync("E-1001", "Asha Rao");

        await UpdateAsync(created.Value.Id, "E-1001", "Asha Rao-Menon", created.Value.ETag);

        var versions = await fixture.ScalarAsync<int>(
            $"SELECT COUNT(*) FROM [Organization].[EmployeeHistory] WHERE [Id] = {created.Value.Id};");

        versions.ShouldBeGreaterThan(0, "system versioning must still record the prior version");
    }

    // ------------------------------------------------- Deactivate a leaver

    [Fact]
    public async Task Deactivating_a_leaver_keeps_the_row()
    {
        await fixture.ResetAsync();
        var created = await CreateAsync("E-1001", "Leaver");

        var result = await DeactivateAsync(created.Value.Id, created.Value.ETag);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeFalse();
        (await GetAsync(created.Value.Id)).IsSuccess
            .ShouldBeTrue("assets, tickets and history still point at this row");
    }

    [Fact]
    public async Task Deactivating_a_manager_detaches_their_direct_reports()
    {
        // Otherwise an approval chain quietly points at somebody who has left.
        await fixture.ResetAsync();
        var manager = await CreateAsync("E-0001", "Manager");
        var first = await CreateAsync("E-0002", "First Report", managerId: manager.Value.Id);
        var second = await CreateAsync("E-0003", "Second Report", managerId: manager.Value.Id);

        var result = await DeactivateAsync(manager.Value.Id, manager.Value.ETag);

        result.Value.DirectReportsReassigned.ShouldBe(2);
        (await GetAsync(first.Value.Id)).Value.ReportingManagerId.ShouldBeNull();
        (await GetAsync(second.Value.Id)).Value.ReportingManagerId.ShouldBeNull();
    }

    [Fact]
    public async Task Deactivating_with_a_stale_stamp_changes_nothing()
    {
        await fixture.ResetAsync();
        var created = await CreateAsync("E-1001", "Leaver");
        var stale = created.Value.ETag;
        await UpdateAsync(created.Value.Id, "E-1001", "Renamed", stale);

        var result = await DeactivateAsync(created.Value.Id, stale);

        result.IsSuccess.ShouldBeFalse();
        (await GetAsync(created.Value.Id)).Value.IsActive.ShouldBeTrue();
    }

    // ------------------------------------------------------------ helpers

    private async Task<(int DepartmentId, int BranchId)> SeedMasterDataAsync()
    {
        await using var context = fixture.NewContext();

        var department = new Domain.Department
        {
            DepartmentName = "IT",
            IsActive = true,
            CreatedOnUtc = fixture.Clock.UtcNow,
            CreatedBy = "test",
        };
        var branch = new Domain.Branch
        {
            BranchCode = "BLR",
            BranchName = "Bangalore",
            TimeZoneId = "India Standard Time",
            IsHeadOffice = false,
            IsActive = true,
            CreatedOnUtc = fixture.Clock.UtcNow,
            CreatedBy = "test",
        };

        context.Departments.Add(department);
        context.Branches.Add(branch);
        await context.SaveChangesAsync();

        return (department.Id, branch.Id);
    }

    private async Task<Result<CreateEmployeeResponse>> CreateAsync(
        string code, string name, int? departmentId = null, int? branchId = null, int? managerId = null)
    {
        await using var context = fixture.NewContext();
        return await new CreateEmployeeHandler(context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(
                new CreateEmployeeCommand(
                    code.ToUpperInvariant(), name, null, null, departmentId, branchId, managerId),
                TestContext.Current.CancellationToken);
    }

    private async Task<Result<UpdateEmployeeResponse>> UpdateAsync(
        int id, string code, string name, string etag, int? managerId = null)
    {
        await using var context = fixture.NewContext();
        return await new UpdateEmployeeHandler(context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(
                new UpdateEmployeeCommand(id, code, name, null, null, null, null, managerId, etag),
                TestContext.Current.CancellationToken);
    }

    private async Task<Result<DeactivateEmployeeResponse>> DeactivateAsync(int id, string etag)
    {
        await using var context = fixture.NewContext();
        return await new DeactivateEmployeeHandler(context, fixture.Clock, fixture.CurrentUser)
            .HandleAsync(new DeactivateEmployeeCommand(id, etag), TestContext.Current.CancellationToken);
    }

    private async Task<Result<GetEmployeeResponse>> GetAsync(int id)
    {
        await using var context = fixture.NewContext();
        return await new GetEmployeeHandler(context)
            .HandleAsync(new GetEmployeeQuery(id), TestContext.Current.CancellationToken);
    }

    private async Task<Result<SearchEmployeesResponse>> SearchAsync(
        string? search = null, int? departmentId = null, int? branchId = null,
        bool? isActive = null, int skip = 0, int take = 50)
    {
        await using var context = fixture.NewContext();
        return await new SearchEmployeesHandler(context)
            .HandleAsync(
                new SearchEmployeesQuery(search, departmentId, branchId, isActive, skip, take),
                TestContext.Current.CancellationToken);
    }
}
