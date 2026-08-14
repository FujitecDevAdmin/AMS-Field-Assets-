using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.UpdateRole;

/// <summary>
/// Rename a role or retire it.
/// </summary>
/// <remarks>
/// Deactivating rather than deleting is deliberate: <c>GetUserCapabilities</c>
/// ignores inactive roles, so retiring one withdraws what it granted while
/// leaving the record of who held it intact.
/// </remarks>
public sealed class UpdateRoleHandler(
    IdentityDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateRoleCommand, UpdateRoleResponse>
{
    public async Task<Result<UpdateRoleResponse>> HandleAsync(UpdateRoleCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var role = await db.Roles.SingleOrDefaultAsync(r => r.Id == request.RoleId, ct);
        if (role is null)
        {
            return Error.NotFound("Role", request.RoleId);
        }

        if (role.IsSystemRole && !request.IsActive)
        {
            // The application depends on it existing and granting what it grants.
            return Error.Validation(
                "Role.SystemRoleCannotBeDeactivated",
                $"'{role.RoleName}' is a system role and cannot be deactivated.");
        }

        role.RoleName = request.RoleName;
        role.Description = request.Description;
        role.IsActive = request.IsActive;
        role.ModifiedOnUtc = clock.UtcNow;
        role.ModifiedBy = currentUser.Username;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        return new UpdateRoleResponse(role.Id, role.RoleName, role.IsActive);
    }
}
