using AMS.Modules.Identity.Domain;
using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.AssignUserRoles;

/// <summary>
/// Replace the roles a user holds. Catalogue: Assign roles.
/// </summary>
/// <remarks>
/// The command carries the complete set, not a delta. Two administrators
/// editing the same user with add/remove operations produce an order-dependent
/// result; sending the whole set makes the last writer's intent the one that
/// survives, which is at least explicable.
/// </remarks>
public sealed class AssignUserRolesHandler(
    IdentityDbContext db,
    IClock clock,
    ICurrentUser currentUser) : IRequestHandler<AssignUserRolesCommand, AssignUserRolesResponse>
{
    public async Task<Result<AssignUserRolesResponse>> HandleAsync(
        AssignUserRolesCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Error.NotFound("User", request.UserId);
        }

        var wanted = request.RoleIds.Distinct().ToList();

        // Roles are in THIS schema, so a missing one is a real foreign key
        // violation. Checking here turns a 500 into a sentence a person can act
        // on, which is the one case 03 §1 rule 6 does not cover: the FK cannot
        // tell us WHICH id was wrong.
        var existing = await db.Roles.Where(r => wanted.Contains(r.Id)).Select(r => r.Id).ToListAsync(ct);
        var unknown = wanted.Except(existing).ToList();
        if (unknown.Count > 0)
        {
            return Error.Validation(
                "Role.NotFound",
                $"No such role: {string.Join(", ", unknown)}.");
        }

        var current = await db.UserRoles.Where(r => r.UserId == request.UserId).ToListAsync(ct);

        db.UserRoles.RemoveRange(current.Where(r => !wanted.Contains(r.RoleId)));

        foreach (var roleId in wanted.Where(id => current.TrueForAll(r => r.RoleId != id)))
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = request.UserId,
                RoleId = roleId,
                GrantedOnUtc = clock.UtcNow,
                GrantedBy = currentUser.Username,
            });
        }

        await db.SaveChangesAsync(ct);

        return new AssignUserRolesResponse(request.UserId, wanted);
    }
}
