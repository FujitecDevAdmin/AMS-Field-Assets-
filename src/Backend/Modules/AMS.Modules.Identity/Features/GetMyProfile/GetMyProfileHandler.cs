using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.GetMyProfile;

/// <summary>
/// The signed-in user's own record. Catalogue screen: My Profile.
/// </summary>
/// <remarks>
/// Reads only what the profile screen shows. Never the password hash, never
/// the MFA secret — 03 §8 keeps those out of any projection that reaches a
/// client, and "it is only the profile screen" is how they leak.
/// </remarks>
public sealed class GetMyProfileHandler(IdentityDbContext db)
    : IRequestHandler<GetMyProfileQuery, GetMyProfileResponse>
{
    public async Task<Result<GetMyProfileResponse>> HandleAsync(
        GetMyProfileQuery request,
        CancellationToken ct)
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
                u.MustChangePassword,
                u.MfaEnabled,
                u.HasAllBranches,
            })
            .SingleOrDefaultAsync(ct);

        if (user is null)
        {
            return Error.NotFound("User", request.UserId);
        }

        var remaining = await db.UserRecoveryCodes
            .AsNoTracking()
            .CountAsync(c => c.UserId == request.UserId && c.UsedOnUtc == null, ct);

        var branchIds = user.HasAllBranches
            ? []
            : await db.UserBranches
                .AsNoTracking()
                .Where(b => b.UserId == request.UserId)
                .Select(b => b.LocationId)
                .OrderBy(id => id)
                .ToListAsync(ct);

        return new GetMyProfileResponse(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Email,
            user.MustChangePassword,
            user.MfaEnabled,
            remaining,
            user.HasAllBranches,
            branchIds);
    }
}
