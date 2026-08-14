using AMS.Modules.Identity.Authentication;
using AMS.Modules.Identity.Persistence;
using AMS.Modules.Identity.PublicApi.Identity;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.PublicApi;

/// <summary>Identity's answer to "who is this, and who holds that".</summary>
/// <remarks>
/// Read-only, and the interface says so. Another module may need to write to
/// somebody; none of them may create a user or grant a capability. Those stay
/// behind Identity's own slices and Identity's own capabilities, where an
/// administrator can see them happen.
/// </remarks>
public sealed class UserDirectory(IdentityDbContext db, EffectiveAccess access) : IUserDirectory
{
    public async Task<UserContact?> FindAsync(int userId, CancellationToken ct) =>
        await Project(db.Users.Where(u => u.Id == userId)).SingleOrDefaultAsync(ct);

    public async Task<UserContact?> ForEmployeeAsync(int employeeId, CancellationToken ct) =>
        await Project(db.Users.Where(u => u.EmployeeId == employeeId && u.IsActive))
            .OrderBy(u => u.UserId)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<UserContact>> InRoleAsync(int roleId, CancellationToken ct) =>
        await Project(db.Users.Where(u =>
            u.IsActive && db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId)))
            .OrderBy(u => u.UserId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<UserContact>> WithCapabilityAsync(
        string capabilityName,
        int? branchId,
        CancellationToken ct)
    {
        // EffectiveAccess owns the rule — role grants, per-user grants, and a
        // deny that beats both. Repeating it here would be a second answer to
        // "may they", and the two would drift.
        var ids = await access.UsersWithAsync(capabilityName, branchId, ct);

        return await Project(db.Users.Where(u => ids.Contains(u.Id)))
            .OrderBy(u => u.UserId)
            .ToListAsync(ct);
    }

    private static IQueryable<UserContact> Project(IQueryable<Domain.User> users) =>
        users.AsNoTracking()
            .Select(u => new UserContact(u.Id, u.EmployeeId, u.DisplayName, u.Email));
}
