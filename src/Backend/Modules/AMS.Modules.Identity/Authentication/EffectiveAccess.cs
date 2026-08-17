using AMS.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Authentication;

/// <summary>
/// What one user may see and do, once roles, per-user overrides and branch
/// scope have been resolved.
/// </summary>
/// <param name="HasAllBranches">Head office. When true the branch list is empty and unused.</param>
/// <param name="BranchIds">The branches this login may see.</param>
/// <param name="Capabilities">The effective set, deny already applied.</param>
public sealed record EffectiveAccessSet(
    bool HasAllBranches,
    IReadOnlyList<int> BranchIds,
    IReadOnlyList<string> Capabilities);

/// <summary>
/// Resolves a user's effective capabilities and branch scope.
/// </summary>
/// <remarks>
/// <para>
/// Extracted so the sign-in slices and <c>GetUserCapabilities</c> cannot
/// disagree. They must not: the query is what an administrator reads off the
/// screen to check somebody's access, and the sign-in path is what actually
/// grants it. Two copies of "a deny beats a role grant" is two chances for the
/// screen to be a lie.
/// </para>
/// <para>
/// No module asks this class whether the CALLER may do something — that is
/// read from the token, resolved once at sign-in. What another module may ask,
/// through <c>IUserDirectory</c>, is who ELSE holds a capability: approval
/// routing has to address the people who can approve, and SLA escalation the
/// people who can be escalated to. Answering that is
/// <see cref="UsersWithAsync"/>, and it lives here so the deny rule below has
/// exactly one implementation.
/// </para>
/// </remarks>
public sealed class EffectiveAccess(IdentityDbContext db)
{
    /// <summary>Resolves for one user. The user is assumed to exist.</summary>
    public async Task<EffectiveAccessSet> ResolveAsync(
        int userId,
        bool hasAllBranches,
        CancellationToken ct)
    {
        // Granted by any ACTIVE role the user holds. An inactive role grants
        // nothing, which is how a role is retired without unpicking it.
        var fromRoles = await db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(db.Roles.Where(r => r.IsActive), ur => ur.RoleId, r => r.Id, (ur, r) => r.Id)
            .Join(db.RoleCapabilities, roleId => roleId, rc => rc.RoleId, (_, rc) => rc.CapabilityName)
            .Distinct()
            .ToListAsync(ct);

        var overrides = await db.UserCapabilityOverrides
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(o => new { o.CapabilityName, o.IsGranted })
            .ToListAsync(ct);

        var denied = overrides
            .Where(o => !o.IsGranted)
            .Select(o => o.CapabilityName)
            .ToHashSet(StringComparer.Ordinal);
        var granted = overrides.Where(o => o.IsGranted).Select(o => o.CapabilityName);

        // A deny beats every role grant, in both directions, so that one
        // permission can be withdrawn from one person without touching their
        // roles. Applied LAST for exactly that reason.
        var effective = fromRoles
            .Concat(granted)
            .Where(name => !denied.Contains(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var branchIds = hasAllBranches
            ? []
            : await db.UserBranches
                .AsNoTracking()
                .Where(b => b.UserId == userId)
                .Select(b => b.BranchId)
                .OrderBy(id => id)
                .ToListAsync(ct);

        return new EffectiveAccessSet(hasAllBranches, branchIds, effective);
    }

    /// <summary>
    /// Everybody who effectively holds a capability, optionally narrowed to
    /// those who can act at one branch.
    /// </summary>
    /// <param name="capabilityName">Spelled as the seed spells it.</param>
    /// <param name="branchId">
    /// When given: users scoped to that branch, plus users with all-branch
    /// access, who are able to act anywhere by definition.
    /// </param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// The same three rules as <see cref="ResolveAsync"/> — an inactive role
    /// grants nothing, an override grants, a deny beats both — asked the other
    /// way round. Set-based rather than a loop over users calling
    /// <see cref="ResolveAsync"/>: an approval route resolved user by user
    /// would be one query per employee in the company.
    /// </remarks>
    public async Task<IReadOnlyList<int>> UsersWithAsync(
        string capabilityName,
        int? branchId,
        CancellationToken ct)
    {
        var fromRoles = db.UserRoles
            .Where(ur => db.Roles.Any(r => r.Id == ur.RoleId && r.IsActive))
            .Where(ur => db.RoleCapabilities.Any(
                rc => rc.RoleId == ur.RoleId && rc.CapabilityName == capabilityName))
            .Select(ur => ur.UserId);

        var granted = db.UserCapabilityOverrides
            .Where(o => o.CapabilityName == capabilityName && o.IsGranted)
            .Select(o => o.UserId);

        var candidates = fromRoles.Union(granted);

        var query = db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && candidates.Contains(u.Id))
            .Where(u => !db.UserCapabilityOverrides.Any(
                o => o.UserId == u.Id
                    && o.CapabilityName == capabilityName
                    && !o.IsGranted));

        if (branchId is { } branch)
        {
            query = query.Where(u =>
                u.HasAllBranches
                || db.UserBranches.Any(b => b.UserId == u.Id && b.BranchId == branch));
        }

        return await query.Select(u => u.Id).OrderBy(id => id).ToListAsync(ct);
    }
}
