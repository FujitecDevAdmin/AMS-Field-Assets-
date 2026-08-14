using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.GetUser;

/// <summary>One user, as the Users screen edits them.</summary>
public sealed class GetUserHandler(IdentityDbContext db)
    : IRequestHandler<GetUserQuery, GetUserResponse>
{
    public async Task<Result<GetUserResponse>> HandleAsync(GetUserQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.DisplayName,
                u.Email,
                u.EmployeeId,
                u.IsActive,
                u.IsLocked,
                u.MustChangePassword,
                u.MfaEnabled,
                u.HasAllBranches,
                u.RowVersion,
            })
            .SingleOrDefaultAsync(ct);

        if (user is null)
        {
            return Error.NotFound("User", request.UserId);
        }

        var roleIds = await db.UserRoles
            .AsNoTracking()
            .Where(r => r.UserId == request.UserId)
            .Select(r => r.RoleId)
            .OrderBy(id => id)
            .ToListAsync(ct);

        var branches = await db.UserBranches
            .AsNoTracking()
            .Where(b => b.UserId == request.UserId)
            .Select(b => new { b.LocationId, b.IsPrimary })
            .OrderBy(b => b.LocationId)
            .ToListAsync(ct);

        return new GetUserResponse(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Email,
            user.EmployeeId,
            user.IsActive,
            user.IsLocked,
            user.MustChangePassword,
            user.MfaEnabled,
            user.HasAllBranches,
            roleIds,
            [.. branches.Select(b => b.LocationId)],
            branches.FirstOrDefault(b => b.IsPrimary)?.LocationId,
            Convert.ToBase64String(user.RowVersion));
    }
}
