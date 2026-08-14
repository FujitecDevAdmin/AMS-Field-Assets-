using AMS.Modules.Identity.Domain;
using AMS.Modules.Identity.Features.AssignUserRoles;
using AMS.Modules.Identity.Features.GetUser;
using AMS.Modules.Identity.Features.LockUser;
using AMS.Modules.Identity.Features.ResetUserPassword;
using AMS.Modules.Identity.Features.SearchUsers;
using AMS.Modules.Identity.Features.SetUserBranches;
using AMS.Modules.Identity.Features.UnlockUser;
using AMS.Modules.Identity.Features.UpdateUser;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Tests;

/// <summary>
/// Catalogue screen: Users. Features: Create and maintain users, Assign roles,
/// Set which branches a user sees.
/// </summary>
[Collection(nameof(IdentityCollectionDefinition))]
public sealed class UsersScreenTests(IdentityFixture fixture)
{
    // ------------------------------------------------------ SearchUsers

    [Fact]
    public async Task The_list_pages_and_reports_the_total()
    {
        await fixture.ResetAsync();
        for (var i = 0; i < 7; i++)
        {
            await fixture.AddUserAsync($"user{i:D2}");
        }

        var result = await SearchAsync(new SearchUsersQuery(null, null, 0, 3));

        result.Value.Rows.Count.ShouldBe(3);
        result.Value.TotalCount.ShouldBe(7, "the total ignores paging");
    }

    [Fact]
    public async Task The_list_filters_by_search_term_and_active_flag()
    {
        await fixture.ResetAsync();
        await fixture.AddUserAsync("alice.smith");
        await fixture.AddUserAsync("bob.smith", isActive: false);
        await fixture.AddUserAsync("carol.jones");

        (await SearchAsync(new SearchUsersQuery("smith", null, 0, 50))).Value.TotalCount.ShouldBe(2);
        (await SearchAsync(new SearchUsersQuery("smith", true, 0, 50))).Value.TotalCount.ShouldBe(1);
        (await SearchAsync(new SearchUsersQuery(null, false, 0, 50))).Value.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_page_past_the_end_is_empty_rather_than_an_error()
    {
        await fixture.ResetAsync();
        await fixture.AddUserAsync("only.one");

        var result = await SearchAsync(new SearchUsersQuery(null, null, 500, 50));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Rows.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_list_never_returns_a_password_hash()
    {
        await fixture.ResetAsync();
        await fixture.AddUserAsync("dave");

        var row = (await SearchAsync(new SearchUsersQuery(null, null, 0, 50))).Value.Rows.Single();

        typeof(SearchUsersResponse.Row).GetProperties()
            .ShouldNotContain(p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        row.Username.ShouldBe("dave");
    }

    // --------------------------------------------------------- GetUser

    [Fact]
    public async Task One_user_comes_back_with_roles_branches_and_an_etag()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("erin");
        var role = await AddRoleAsync("BranchAdmin");
        await AssignRolesAsync(user.Id, [role.Id]);
        await SetBranchesAsync(user.Id, [4, 9], primary: 9);

        var result = await GetUserAsync(user.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.RoleIds.ShouldBe([role.Id]);
        result.Value.BranchIds.ShouldBe([4, 9]);
        result.Value.PrimaryBranchId.ShouldBe(9);
        result.Value.ETag.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task An_unknown_user_is_not_found()
    {
        await fixture.ResetAsync();

        (await GetUserAsync(4242)).Error!.Code.ShouldBe("User.NotFound");
    }

    // ------------------------------------------------------ UpdateUser

    [Fact]
    public async Task An_edit_with_the_current_etag_succeeds_and_returns_a_new_one()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("frank");
        var etag = (await GetUserAsync(user.Id)).Value.ETag;

        var result = await UpdateAsync(user.Id, "Frank Renamed", etag);

        result.IsSuccess.ShouldBeTrue();
        result.Value.DisplayName.ShouldBe("Frank Renamed");
        result.Value.ETag.ShouldNotBe(etag, "the version must move when the row changes");
    }

    [Fact]
    public async Task An_edit_with_a_stale_etag_is_a_412_and_changes_nothing()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("grace");
        var stale = (await GetUserAsync(user.Id)).Value.ETag;

        await UpdateAsync(user.Id, "First writer wins", stale);
        var result = await UpdateAsync(user.Id, "Second writer loses", stale);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Kind.ShouldBe(ErrorKind.Concurrency);
        (await GetUserAsync(user.Id)).Value.DisplayName.ShouldBe("First writer wins");
    }

    [Fact]
    public async Task A_malformed_etag_is_a_validation_error_not_a_crash()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("heidi");

        var result = await UpdateAsync(user.Id, "Heidi", "this is not base64!!");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Kind.ShouldBe(ErrorKind.Validation);
    }

    // -------------------------------------------------- Lock and unlock

    [Fact]
    public async Task Locking_and_unlocking_move_the_flag()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("ivan");

        (await LockAsync(user.Id)).Value.IsLocked.ShouldBeTrue();
        (await fixture.ReloadAsync(user.Id)).IsLocked.ShouldBeTrue();

        (await UnlockAsync(user.Id)).Value.IsLocked.ShouldBeFalse();
        (await fixture.ReloadAsync(user.Id)).IsLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task Unlocking_also_clears_the_failure_count()
    {
        // Otherwise the next single typo locks the account again and the
        // administrator gets a second call five minutes later.
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("judy", isLocked: true);
        await fixture.ExecuteAsync($"UPDATE [Identity].[User] SET [FailedLoginAttempts] = 5 WHERE [Id] = {user.Id};");

        var result = await UnlockAsync(user.Id);

        result.Value.FailedLoginAttempts.ShouldBe(0);
        (await fixture.ReloadAsync(user.Id)).FailedLoginAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task An_administrator_cannot_lock_themselves_out()
    {
        await fixture.ResetAsync();
        var me = await fixture.AddUserAsync("self.locker");
        fixture.CurrentUser.Id = me.Id;

        var result = await LockAsync(me.Id);
        fixture.CurrentUser.Id = 1;

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("User.CannotLockSelf");
    }

    [Fact]
    public async Task Locking_an_already_locked_account_is_not_an_error()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("ken", isLocked: true);

        (await LockAsync(user.Id)).IsSuccess.ShouldBeTrue("the caller asked for a state, not a transition");
    }

    // -------------------------------------------------- ResetUserPassword

    [Fact]
    public async Task A_reset_forces_a_change_and_clears_the_failure_count()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("laura");
        await fixture.ExecuteAsync($"UPDATE [Identity].[User] SET [FailedLoginAttempts] = 3 WHERE [Id] = {user.Id};");

        var result = await ResetPasswordAsync(user.Id, "a temporary passphrase");

        result.Value.MustChangePassword.ShouldBeTrue();

        var reloaded = await fixture.ReloadAsync(user.Id);
        reloaded.FailedLoginAttempts.ShouldBe(0);
        fixture.Hasher.Verify("a temporary passphrase", reloaded.PasswordHash).ShouldBeTrue();
    }

    // --------------------------------------------------- AssignUserRoles

    [Fact]
    public async Task Assigning_roles_replaces_the_whole_set()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("mallory");
        var a = await AddRoleAsync("RoleA");
        var b = await AddRoleAsync("RoleB");
        var c = await AddRoleAsync("RoleC");

        await AssignRolesAsync(user.Id, [a.Id, b.Id]);
        var result = await AssignRolesAsync(user.Id, [b.Id, c.Id]);

        result.Value.RoleIds.ShouldBe([b.Id, c.Id]);
        (await GetUserAsync(user.Id)).Value.RoleIds.OrderBy(id => id)
            .ShouldBe(new[] { b.Id, c.Id }.OrderBy(id => id));
    }

    [Fact]
    public async Task Assigning_an_unknown_role_is_refused_by_name()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("niaj");

        var result = await AssignRolesAsync(user.Id, [8888]);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Role.NotFound");
        result.Error.Message.ShouldContain("8888");
    }

    [Fact]
    public async Task Assigning_the_same_role_twice_stores_it_once()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("olivia");
        var role = await AddRoleAsync("Duplicated");

        var result = await AssignRolesAsync(user.Id, [role.Id, role.Id]);

        result.IsSuccess.ShouldBeTrue();
        await using var context = fixture.NewContext();
        (await context.UserRoles.CountAsync(r => r.UserId == user.Id)).ShouldBe(1);
    }

    [Fact]
    public async Task Assigning_an_empty_set_removes_every_role()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("peggy");
        var role = await AddRoleAsync("Temporary");
        await AssignRolesAsync(user.Id, [role.Id]);

        await AssignRolesAsync(user.Id, []);

        (await GetUserAsync(user.Id)).Value.RoleIds.ShouldBeEmpty();
    }

    // --------------------------------------------------- SetUserBranches

    [Fact]
    public async Task Setting_branches_replaces_the_set_and_moves_the_primary()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("quentin");

        await SetBranchesAsync(user.Id, [1, 2], primary: 1);
        var result = await SetBranchesAsync(user.Id, [2, 3], primary: 3);

        result.IsSuccess.ShouldBeTrue();
        var reloaded = await GetUserAsync(user.Id);
        reloaded.Value.BranchIds.ShouldBe([2, 3]);
        reloaded.Value.PrimaryBranchId.ShouldBe(3);
    }

    [Fact]
    public async Task Only_one_branch_is_ever_primary()
    {
        // UX_UserBranch_OnePrimary is the guarantee; this proves the handler
        // works with it rather than around it.
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("rupert");

        await SetBranchesAsync(user.Id, [1, 2, 3], primary: 2);

        await using var context = fixture.NewContext();
        (await context.UserBranches.CountAsync(b => b.UserId == user.Id && b.IsPrimary)).ShouldBe(1);
    }

    [Fact]
    public async Task Branches_with_no_primary_are_allowed()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("sybil");

        var result = await SetBranchesAsync(user.Id, [5, 6], primary: null);

        result.IsSuccess.ShouldBeTrue();
        (await GetUserAsync(user.Id)).Value.PrimaryBranchId.ShouldBeNull();
    }

    [Fact]
    public async Task Setting_an_empty_branch_list_removes_them_all()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("trent");
        await SetBranchesAsync(user.Id, [1], primary: 1);

        await SetBranchesAsync(user.Id, [], primary: null);

        (await GetUserAsync(user.Id)).Value.BranchIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task Duplicate_branch_ids_are_stored_once()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("uma");

        var result = await SetBranchesAsync(user.Id, [7, 7, 7], primary: 7);

        result.IsSuccess.ShouldBeTrue("the composite primary key would otherwise reject the insert");
        (await GetUserAsync(user.Id)).Value.BranchIds.ShouldBe([7]);
    }

    // ------------------------------------------------------------ helpers

    private async Task<Role> AddRoleAsync(string name)
    {
        await using var context = fixture.NewContext();
        var role = new Role
        {
            RoleName = name,
            IsActive = true,
            IsSystemRole = false,
            CreatedOnUtc = fixture.Clock.UtcNow,
            CreatedBy = "test",
        };
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        return role;
    }

    private async Task<Result<SearchUsersResponse>> SearchAsync(SearchUsersQuery query)
    {
        await using var context = fixture.NewContext();
        return await new SearchUsersHandler(context).HandleAsync(query, TestContext.Current.CancellationToken);
    }

    private async Task<Result<GetUserResponse>> GetUserAsync(int userId)
    {
        await using var context = fixture.NewContext();
        return await new GetUserHandler(context)
            .HandleAsync(new GetUserQuery(userId), TestContext.Current.CancellationToken);
    }

    private async Task<Result<UpdateUserResponse>> UpdateAsync(int userId, string displayName, string etag)
    {
        await using var context = fixture.NewContext();
        return await new UpdateUserHandler(context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(
                new UpdateUserCommand(userId, displayName, null, null, true, false, etag),
                TestContext.Current.CancellationToken);
    }

    private async Task<Result<LockUserResponse>> LockAsync(int userId)
    {
        await using var context = fixture.NewContext();
        return await new LockUserHandler(context, fixture.Clock, fixture.CurrentUser)
            .HandleAsync(new LockUserCommand(userId, "because"), TestContext.Current.CancellationToken);
    }

    private async Task<Result<UnlockUserResponse>> UnlockAsync(int userId)
    {
        await using var context = fixture.NewContext();
        return await new UnlockUserHandler(context, fixture.Clock, fixture.CurrentUser)
            .HandleAsync(new UnlockUserCommand(userId), TestContext.Current.CancellationToken);
    }

    private async Task<Result<ResetUserPasswordResponse>> ResetPasswordAsync(int userId, string password)
    {
        await using var context = fixture.NewContext();
        return await new ResetUserPasswordHandler(context, fixture.Hasher, fixture.Clock, fixture.CurrentUser)
            .HandleAsync(new ResetUserPasswordCommand(userId, password), TestContext.Current.CancellationToken);
    }

    private async Task<Result<AssignUserRolesResponse>> AssignRolesAsync(int userId, IReadOnlyList<int> roleIds)
    {
        await using var context = fixture.NewContext();
        return await new AssignUserRolesHandler(context, fixture.Clock, fixture.CurrentUser)
            .HandleAsync(new AssignUserRolesCommand(userId, roleIds), TestContext.Current.CancellationToken);
    }

    private async Task<Result<SetUserBranchesResponse>> SetBranchesAsync(
        int userId, IReadOnlyList<int> branchIds, int? primary)
    {
        await using var context = fixture.NewContext();
        return await new SetUserBranchesHandler(context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(
                new SetUserBranchesCommand(userId, branchIds, primary),
                TestContext.Current.CancellationToken);
    }
}
