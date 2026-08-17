using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.ListAuditorAccounts;

public sealed class ListAuditorAccountsHandler(IdentityDbContext db)
    : IRequestHandler<ListAuditorAccountsQuery, ListAuditorAccountsResponse>
{
    public async Task<Result<ListAuditorAccountsResponse>> HandleAsync(
        ListAuditorAccountsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var roleId = await db.Roles.AsNoTracking().Where(role => role.RoleName == "Auditor")
            .Select(role => (int?)role.Id).SingleOrDefaultAsync(ct);
        if (!roleId.HasValue)
        {
            return new ListAuditorAccountsResponse([]);
        }

        var rows = await db.UserRoles.AsNoTracking().Where(userRole => userRole.RoleId == roleId.Value)
            .Join(db.Users.AsNoTracking(), userRole => userRole.UserId, user => user.Id, (_, user) => user)
            .OrderBy(user => user.DisplayName)
            .Select(user => new ListAuditorAccountsResponse.Row(
                user.Id, user.Username, user.DisplayName, user.Email, user.EmployeeId,
                user.HasAllBranches,
                db.UserBranches.Where(branch => branch.UserId == user.Id)
                    .OrderByDescending(branch => branch.IsPrimary).Select(branch => branch.BranchId).ToList(),
                user.IsActive, user.IsLocked, user.MfaEnabled, user.LastLoginOnUtc))
            .ToListAsync(ct);
        return new ListAuditorAccountsResponse(rows);
    }
}
