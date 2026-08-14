using AMS.Modules.Identity.Domain;
using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.CreateRole;

/// <summary>
/// Add a role. Catalogue screen: Roles &amp; Capabilities.
/// </summary>
/// <remarks>
/// This is also how "Field Asset Admin access" is built: a role holding the
/// field-asset capabilities. There is no separate field-asset login, and no
/// code specific to it.
/// </remarks>
public sealed class CreateRoleHandler(
    IdentityDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CreateRoleCommand, CreateRoleResponse>
{
    public async Task<Result<CreateRoleResponse>> HandleAsync(CreateRoleCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var role = new Role
        {
            RoleName = request.RoleName,
            Description = request.Description,

            // Only the schema's own seed creates system roles.
            IsSystemRole = false,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.Roles.Add(role);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            // UX_Role_Name decides, not a read-then-write check.
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        return new CreateRoleResponse(role.Id, role.RoleName);
    }
}
