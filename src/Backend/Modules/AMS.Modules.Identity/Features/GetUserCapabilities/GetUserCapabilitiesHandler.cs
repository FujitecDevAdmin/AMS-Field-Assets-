using AMS.Modules.Identity.Authentication;
using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.GetUserCapabilities;

/// <summary>
/// The reference query handler, and the rule that every other module's
/// authorisation depends on.
/// </summary>
/// <remarks>
/// <c>AsNoTracking</c>, projection straight to the Response, no
/// <c>SaveChanges</c> (docs/02 §4).
///
/// The resolution itself moved to <see cref="EffectiveAccess"/> so this query
/// and the sign-in path cannot disagree about who may do what.
/// </remarks>
public sealed class GetUserCapabilitiesHandler(IdentityDbContext db, EffectiveAccess effectiveAccess)
    : IRequestHandler<GetUserCapabilitiesQuery, GetUserCapabilitiesResponse>
{
    public async Task<Result<GetUserCapabilitiesResponse>> HandleAsync(
        GetUserCapabilitiesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId && !u.IsLocked && u.IsActive)
            .Select(u => new { u.Id, u.Username, u.HasAllBranches })
            .SingleOrDefaultAsync(ct);

        if (user is null)
        {
            return Error.NotFound("User", request.UserId);
        }

        var access = await effectiveAccess.ResolveAsync(user.Id, user.HasAllBranches, ct);

        return new GetUserCapabilitiesResponse(
            user.Id,
            user.Username,
            access.HasAllBranches,
            access.BranchIds,
            access.Capabilities);
    }
}
