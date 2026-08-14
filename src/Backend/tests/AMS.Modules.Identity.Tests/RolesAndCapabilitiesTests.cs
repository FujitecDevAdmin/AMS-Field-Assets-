using AMS.Modules.Identity.Domain;
using AMS.Modules.Identity.Features.AssignUserRoles;
using AMS.Modules.Identity.Features.CreateRole;
using AMS.Modules.Identity.Features.GetCapabilities;
using AMS.Modules.Identity.Features.GetUserCapabilities;
using AMS.Modules.Identity.Features.SearchRoles;
using AMS.Modules.Identity.Features.SetRoleCapabilities;
using AMS.Modules.Identity.Features.SetUserCapabilityOverride;
using AMS.Modules.Identity.Features.UpdateRole;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Tests;

/// <summary>
/// Catalogue screen: Roles and Capabilities. Features: Grant or deny one
/// capability, and Field Asset Admin access — which is a role holding the
/// field-asset capabilities and needs no code of its own.
/// </summary>
[Collection(nameof(IdentityCollectionDefinition))]
public sealed class RolesAndCapabilitiesTests(IdentityFixture fixture)
{
    // ---------------------------------------------------------- CreateRole

    [Fact]
    public async Task A_role_can_be_created()
    {
        await fixture.ResetAsync();

        var result = await CreateRoleAsync("BranchAdmin");

        result.IsSuccess.ShouldBeTrue();
        result.Value.RoleName.ShouldBe("BranchAdmin");
    }

    [Fact]
    public async Task Two_roles_cannot_share_a_name()
    {
        // UX_Role_Name decides, not a read-then-write check.
        await fixture.ResetAsync();
        await CreateRoleAsync("Technician");

        var result = await CreateRoleAsync("Technician");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Kind.ShouldBe(ErrorKind.Conflict);
    }

    [Fact]
    public async Task A_created_role_is_active_and_not_a_system_role()
    {
        await fixture.ResetAsync();
        var created = await CreateRoleAsync("Auditor");

        await using var context = fixture.NewContext();
        var role = await context.Roles.SingleAsync(r => r.Id == created.Value.Id);

        role.IsActive.ShouldBeTrue();
        role.IsSystemRole.ShouldBeFalse("only the schema seed creates system roles");
    }

    // ---------------------------------------------------------- UpdateRole

    [Fact]
    public async Task A_role_can_be_renamed_and_retired()
    {
        await fixture.ResetAsync();
        var created = await CreateRoleAsync("Temporary");

        var result = await UpdateRoleAsync(created.Value.Id, "Retired", isActive: false);

        result.IsSuccess.ShouldBeTrue();
        result.Value.RoleName.ShouldBe("Retired");
        result.Value.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task An_unknown_role_cannot_be_updated()
    {
        await fixture.ResetAsync();

        (await UpdateRoleAsync(7777, "Ghost", true)).Error!.Code.ShouldBe("Role.NotFound");
    }

    [Fact]
    public async Task A_system_role_cannot_be_deactivated()
    {
        await fixture.ResetAsync();
        var role = await AddRoleAsync("SuperAdmin", isSystemRole: true);

        var result = await UpdateRoleAsync(role.Id, "SuperAdmin", isActive: false);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Role.SystemRoleCannotBeDeactivated");
    }

    // ------------------------------------------------- SetRoleCapabilities

    [Fact]
    public async Task A_role_grants_the_capabilities_it_is_given()
    {
        await fixture.ResetAsync();
        await AddCapabilityAsync("handover.record");
        await AddCapabilityAsync("handover.dispatch");
        var role = await AddRoleAsync("BranchAdmin");

        var result = await SetRoleCapabilitiesAsync(role.Id, ["handover.record", "handover.dispatch"]);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CapabilityNames.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Capabilities_are_replaced_not_merged()
    {
        await fixture.ResetAsync();
        await AddCapabilityAsync("a.one");
        await AddCapabilityAsync("a.two");
        var role = await AddRoleAsync("Shifting");

        await SetRoleCapabilitiesAsync(role.Id, ["a.one"]);
        await SetRoleCapabilitiesAsync(role.Id, ["a.two"]);

        await using var context = fixture.NewContext();
        var held = await context.RoleCapabilities.Where(rc => rc.RoleId == role.Id).ToListAsync();

        held.Select(h => h.CapabilityName).ShouldBe(["a.two"]);
    }

    [Fact]
    public async Task An_unregistered_capability_is_refused_by_name()
    {
        await fixture.ResetAsync();
        var role = await AddRoleAsync("Hopeful");

        var result = await SetRoleCapabilitiesAsync(role.Id, ["not.registered"]);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Capability.NotFound");
        result.Error.Message.ShouldContain("not.registered");
    }

    [Fact]
    public async Task Clearing_a_role_removes_every_grant()
    {
        await fixture.ResetAsync();
        await AddCapabilityAsync("b.one");
        var role = await AddRoleAsync("Emptied");
        await SetRoleCapabilitiesAsync(role.Id, ["b.one"]);

        await SetRoleCapabilitiesAsync(role.Id, []);

        await using var context = fixture.NewContext();
        (await context.RoleCapabilities.CountAsync(rc => rc.RoleId == role.Id)).ShouldBe(0);
    }

    // -------------------------------------------- the deny-wins resolution

    [Fact]
    public async Task A_user_holds_what_their_roles_grant()
    {
        await fixture.ResetAsync();
        await AddCapabilityAsync("c.read");
        await AddCapabilityAsync("c.write");
        var role = await AddRoleAsync("Writer");
        await SetRoleCapabilitiesAsync(role.Id, ["c.read", "c.write"]);
        var user = await fixture.AddUserAsync("alice");
        await AssignRolesAsync(user.Id, [role.Id]);

        var result = await ResolveAsync(user.Id);

        result.Value.Capabilities.ShouldBe(["c.read", "c.write"]);
    }

    [Fact]
    public async Task A_per_user_deny_beats_a_role_grant()
    {
        // The whole reason overrides exist: one permission withdrawn from one
        // person without unpicking their roles.
        await fixture.ResetAsync();
        await AddCapabilityAsync("d.read");
        await AddCapabilityAsync("d.write");
        var role = await AddRoleAsync("Writer");
        await SetRoleCapabilitiesAsync(role.Id, ["d.read", "d.write"]);
        var user = await fixture.AddUserAsync("bob");
        await AssignRolesAsync(user.Id, [role.Id]);

        await SetOverrideAsync(user.Id, "d.write", isGranted: false);

        (await ResolveAsync(user.Id)).Value.Capabilities.ShouldBe(["d.read"]);
    }

    [Fact]
    public async Task A_per_user_grant_adds_what_no_role_gives()
    {
        await fixture.ResetAsync();
        await AddCapabilityAsync("e.special");
        var user = await fixture.AddUserAsync("carol");

        await SetOverrideAsync(user.Id, "e.special", isGranted: true);

        (await ResolveAsync(user.Id)).Value.Capabilities.ShouldBe(["e.special"]);
    }

    [Fact]
    public async Task A_deny_beats_a_per_user_grant_of_the_same_capability()
    {
        // Setting the same capability twice moves the flag rather than failing
        // on the composite primary key.
        await fixture.ResetAsync();
        await AddCapabilityAsync("f.thing");
        var user = await fixture.AddUserAsync("dave");

        await SetOverrideAsync(user.Id, "f.thing", isGranted: true);
        await SetOverrideAsync(user.Id, "f.thing", isGranted: false);

        (await ResolveAsync(user.Id)).Value.Capabilities.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_inactive_role_grants_nothing()
    {
        // How a role is retired without unpicking who holds it.
        await fixture.ResetAsync();
        await AddCapabilityAsync("g.read");
        var role = await AddRoleAsync("Retiring");
        await SetRoleCapabilitiesAsync(role.Id, ["g.read"]);
        var user = await fixture.AddUserAsync("erin");
        await AssignRolesAsync(user.Id, [role.Id]);

        (await ResolveAsync(user.Id)).Value.Capabilities.ShouldBe(["g.read"]);

        await UpdateRoleAsync(role.Id, "Retiring", isActive: false);

        (await ResolveAsync(user.Id)).Value.Capabilities
            .ShouldBeEmpty("an inactive role must grant nothing");
    }

    [Fact]
    public async Task Two_roles_granting_the_same_capability_yield_it_once()
    {
        await fixture.ResetAsync();
        await AddCapabilityAsync("h.shared");
        var first = await AddRoleAsync("First");
        var second = await AddRoleAsync("Second");
        await SetRoleCapabilitiesAsync(first.Id, ["h.shared"]);
        await SetRoleCapabilitiesAsync(second.Id, ["h.shared"]);
        var user = await fixture.AddUserAsync("frank");
        await AssignRolesAsync(user.Id, [first.Id, second.Id]);

        (await ResolveAsync(user.Id)).Value.Capabilities.ShouldBe(["h.shared"]);
    }

    [Fact]
    public async Task A_locked_user_resolves_to_nothing()
    {
        await fixture.ResetAsync();
        await AddCapabilityAsync("i.read");
        var role = await AddRoleAsync("Reader");
        await SetRoleCapabilitiesAsync(role.Id, ["i.read"]);
        var user = await fixture.AddUserAsync("grace", isLocked: true);
        await AssignRolesAsync(user.Id, [role.Id]);

        (await ResolveAsync(user.Id)).IsSuccess.ShouldBeFalse();
    }

    // ------------------------------------------------------ the overrides

    [Fact]
    public async Task An_override_on_an_unknown_capability_is_refused()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("heidi");

        (await SetOverrideAsync(user.Id, "does.not.exist", true)).Error!.Code
            .ShouldBe("Capability.NotFound");
    }

    [Fact]
    public async Task An_override_for_an_unknown_user_is_refused()
    {
        await fixture.ResetAsync();
        await AddCapabilityAsync("j.read");

        (await SetOverrideAsync(6666, "j.read", true)).Error!.Code.ShouldBe("User.NotFound");
    }

    // ------------------------------------------------- lists for the screen

    [Fact]
    public async Task The_role_list_counts_capabilities_and_holders()
    {
        await fixture.ResetAsync();
        await AddCapabilityAsync("k.one");
        await AddCapabilityAsync("k.two");
        var role = await AddRoleAsync("Counted");
        await SetRoleCapabilitiesAsync(role.Id, ["k.one", "k.two"]);
        var user = await fixture.AddUserAsync("ivan");
        await AssignRolesAsync(user.Id, [role.Id]);

        var row = (await SearchRolesAsync(null)).Value.Rows.Single(r => r.Id == role.Id);

        row.CapabilityCount.ShouldBe(2);
        row.UserCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_role_list_filters_by_active_flag()
    {
        await fixture.ResetAsync();
        await AddRoleAsync("Live");
        var retired = await AddRoleAsync("Gone");
        await UpdateRoleAsync(retired.Id, "Gone", isActive: false);

        (await SearchRolesAsync(true)).Value.Rows.Count.ShouldBe(1);
        (await SearchRolesAsync(false)).Value.Rows.Count.ShouldBe(1);
        (await SearchRolesAsync(null)).Value.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public async Task The_capability_catalogue_can_be_filtered_by_module()
    {
        await fixture.ResetAsync();
        await AddCapabilityAsync("l.one", module: "Allocations");
        await AddCapabilityAsync("l.two", module: "Movements");

        (await GetCapabilitiesAsync(null)).Value.Rows.Count.ShouldBe(2);
        (await GetCapabilitiesAsync("Allocations")).Value.Rows.Single().Name.ShouldBe("l.one");
    }

    // --------------------------------------------------- Field Asset Admin

    [Fact]
    public async Task Field_Asset_Admin_is_an_ordinary_role_holding_field_asset_capabilities()
    {
        // Catalogue feature 10, and the handbook construct deliberately NOT
        // reproduced: there is no second login table. R3 went further and
        // removed the second REGISTER too, so these capabilities now belong to
        // the Assets module and scope a view of the one register.
        await fixture.ResetAsync();
        await AddCapabilityAsync("field-asset.view", module: "Assets");
        await AddCapabilityAsync("field-asset.manage", module: "Assets");

        var role = (await CreateRoleAsync("FieldAssetAdmin")).Value;
        await SetRoleCapabilitiesAsync(role.Id, ["field-asset.view", "field-asset.manage"]);

        var user = await fixture.AddUserAsync("field.admin");
        await AssignRolesAsync(user.Id, [role.Id]);

        (await ResolveAsync(user.Id)).Value.Capabilities
            .ShouldBe(["field-asset.manage", "field-asset.view"]);
    }

    // ------------------------------------------------------------ helpers

    private async Task<Role> AddRoleAsync(string name, bool isSystemRole = false)
    {
        await using var context = fixture.NewContext();
        var role = new Role
        {
            RoleName = name,
            IsActive = true,
            IsSystemRole = isSystemRole,
            CreatedOnUtc = fixture.Clock.UtcNow,
            CreatedBy = "test",
        };
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        return role;
    }

    private async Task AddCapabilityAsync(string name, string module = "Identity")
    {
        await using var context = fixture.NewContext();
        context.Capabilities.Add(new Capability
        {
            Name = name,
            Module = module,
            Description = name,
            CreatedOnUtc = fixture.Clock.UtcNow,
            CreatedBy = "test",
        });
        await context.SaveChangesAsync();
    }

    private async Task<Result<CreateRoleResponse>> CreateRoleAsync(string name)
    {
        await using var context = fixture.NewContext();
        return await new CreateRoleHandler(context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(new CreateRoleCommand(name, null), TestContext.Current.CancellationToken);
    }

    private async Task<Result<UpdateRoleResponse>> UpdateRoleAsync(int roleId, string name, bool isActive)
    {
        await using var context = fixture.NewContext();
        return await new UpdateRoleHandler(context, fixture.Clock, fixture.CurrentUser, fixture.SqlErrors)
            .HandleAsync(new UpdateRoleCommand(roleId, name, null, isActive), TestContext.Current.CancellationToken);
    }

    private async Task<Result<SetRoleCapabilitiesResponse>> SetRoleCapabilitiesAsync(
        int roleId, IReadOnlyList<string> names)
    {
        await using var context = fixture.NewContext();
        return await new SetRoleCapabilitiesHandler(context, fixture.Clock, fixture.CurrentUser)
            .HandleAsync(new SetRoleCapabilitiesCommand(roleId, names), TestContext.Current.CancellationToken);
    }

    private async Task<Result<SetUserCapabilityOverrideResponse>> SetOverrideAsync(
        int userId, string capability, bool isGranted)
    {
        await using var context = fixture.NewContext();
        return await new SetUserCapabilityOverrideHandler(context, fixture.Clock, fixture.CurrentUser)
            .HandleAsync(
                new SetUserCapabilityOverrideCommand(userId, capability, isGranted, "because"),
                TestContext.Current.CancellationToken);
    }

    private async Task<Result<AssignUserRolesResponse>> AssignRolesAsync(int userId, IReadOnlyList<int> roleIds)
    {
        await using var context = fixture.NewContext();
        return await new AssignUserRolesHandler(context, fixture.Clock, fixture.CurrentUser)
            .HandleAsync(new AssignUserRolesCommand(userId, roleIds), TestContext.Current.CancellationToken);
    }

    private async Task<Result<GetUserCapabilitiesResponse>> ResolveAsync(int userId)
    {
        await using var context = fixture.NewContext();
        return await new GetUserCapabilitiesHandler(context, IdentityFixture.NewEffectiveAccess(context))
            .HandleAsync(new GetUserCapabilitiesQuery(userId), TestContext.Current.CancellationToken);
    }

    private async Task<Result<SearchRolesResponse>> SearchRolesAsync(bool? isActive)
    {
        await using var context = fixture.NewContext();
        return await new SearchRolesHandler(context)
            .HandleAsync(new SearchRolesQuery(isActive), TestContext.Current.CancellationToken);
    }

    private async Task<Result<GetCapabilitiesResponse>> GetCapabilitiesAsync(string? module)
    {
        await using var context = fixture.NewContext();
        return await new GetCapabilitiesHandler(context)
            .HandleAsync(new GetCapabilitiesQuery(module), TestContext.Current.CancellationToken);
    }
}
