using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.LockUser;

/// <summary>
/// Lock an account. Catalogue: Create and maintain users.
/// </summary>
/// <remarks>
/// Locking an already-locked account succeeds. The caller asked for a state,
/// not for a transition, and failing would only make the Users grid awkward
/// when two administrators click at once.
/// </remarks>
public sealed class LockUserHandler(
    IdentityDbContext db,
    IClock clock,
    ICurrentUser currentUser) : IRequestHandler<LockUserCommand, LockUserResponse>
{
    public async Task<Result<LockUserResponse>> HandleAsync(LockUserCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Error.NotFound("User", request.UserId);
        }

        if (user.Id == currentUser.Id)
        {
            // An administrator locking themselves out needs another
            // administrator to undo it, and there may not be one.
            return Error.Validation("User.CannotLockSelf", "You cannot lock your own account.");
        }

        user.IsLocked = true;
        user.ModifiedOnUtc = clock.UtcNow;
        user.ModifiedBy = currentUser.Username;

        await db.SaveChangesAsync(ct);

        return new LockUserResponse(user.Id, user.IsLocked);
    }
}
