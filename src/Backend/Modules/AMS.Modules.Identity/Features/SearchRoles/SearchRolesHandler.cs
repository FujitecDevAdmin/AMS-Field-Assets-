using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.SearchRoles;

/// <summary>The role list. Catalogue screen: Roles &amp; Capabilities.</summary>
public sealed class SearchRolesHandler(IdentityDbContext db)
    : IRequestHandler<SearchRolesQuery, SearchRolesResponse>
{
    public async Task<Result<SearchRolesResponse>> HandleAsync(SearchRolesQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Roles.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == request.IsActive.Value);
        }

        // Counted in the same query rather than per row: a list screen that
        // issues one count per role is the N+1 that 03 §6 calls a blocker.
        var rows = await query
            .OrderBy(r => r.RoleName)
            .Select(r => new SearchRolesResponse.Row(
                r.Id,
                r.RoleName,
                r.Description,
                r.IsSystemRole,
                r.IsActive,
                db.RoleCapabilities.Count(rc => rc.RoleId == r.Id),
                db.UserRoles.Count(ur => ur.RoleId == r.Id)))
            .ToListAsync(ct);

        return new SearchRolesResponse(rows);
    }
}
