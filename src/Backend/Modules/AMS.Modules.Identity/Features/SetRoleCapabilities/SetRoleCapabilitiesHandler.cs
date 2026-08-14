using AMS.Modules.Identity.Domain;
using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.SetRoleCapabilities;

/// <summary>
/// Replace what a role grants. Catalogue: the capability matrix.
/// </summary>
/// <remarks>
/// This is the screen that makes "an administrator can move a capability
/// between roles without a release" true, which is the whole reason nothing in
/// the codebase tests a role name.
/// </remarks>
public sealed class SetRoleCapabilitiesHandler(
    IdentityDbContext db,
    IClock clock,
    ICurrentUser currentUser) : IRequestHandler<SetRoleCapabilitiesCommand, SetRoleCapabilitiesResponse>
{
    public async Task<Result<SetRoleCapabilitiesResponse>> HandleAsync(
        SetRoleCapabilitiesCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var role = await db.Roles.SingleOrDefaultAsync(r => r.Id == request.RoleId, ct);
        if (role is null)
        {
            return Error.NotFound("Role", request.RoleId);
        }

        var wanted = request.CapabilityNames.Distinct(StringComparer.Ordinal).ToList();

        // Capability is in this schema and the FK would reject an unknown name,
        // but a 500 saying "FK violation" is not something an administrator can
        // act on. Naming the offending capability is.
        var known = await db.Capabilities
            .Where(c => wanted.Contains(c.Name))
            .Select(c => c.Name)
            .ToListAsync(ct);

        var unknown = wanted.Except(known, StringComparer.Ordinal).ToList();
        if (unknown.Count > 0)
        {
            return Error.Validation(
                "Capability.NotFound",
                $"No such capability: {string.Join(", ", unknown)}. "
                + "Capabilities are registered by the schema seed, not created here.");
        }

        var current = await db.RoleCapabilities.Where(rc => rc.RoleId == request.RoleId).ToListAsync(ct);

        db.RoleCapabilities.RemoveRange(
            current.Where(rc => !wanted.Contains(rc.CapabilityName, StringComparer.Ordinal)));

        foreach (var name in wanted.Where(n => current.TrueForAll(rc => rc.CapabilityName != n)))
        {
            db.RoleCapabilities.Add(new RoleCapability
            {
                RoleId = request.RoleId,
                CapabilityName = name,
                GrantedOnUtc = clock.UtcNow,
                GrantedBy = currentUser.Username,
            });
        }

        await db.SaveChangesAsync(ct);

        return new SetRoleCapabilitiesResponse(request.RoleId, wanted);
    }
}
