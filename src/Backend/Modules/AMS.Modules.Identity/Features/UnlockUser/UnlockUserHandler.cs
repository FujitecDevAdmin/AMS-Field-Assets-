using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.UnlockUser;

/// <summary>
/// Unlock an account. Catalogue: Create and maintain users, Account lockout.
/// </summary>
/// <remarks>
/// The failure count is cleared at the same time. Unlocking while leaving the
/// count at its threshold means the very next typo locks the account again,
/// and the administrator gets a call five minutes after the last one.
/// </remarks>
public sealed class UnlockUserHandler(
    IdentityDbContext db,
    IClock clock,
    ICurrentUser currentUser) : IRequestHandler<UnlockUserCommand, UnlockUserResponse>
{
    public async Task<Result<UnlockUserResponse>> HandleAsync(UnlockUserCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Error.NotFound("User", request.UserId);
        }

        user.IsLocked = false;
        user.FailedLoginAttempts = 0;
        user.ModifiedOnUtc = clock.UtcNow;
        user.ModifiedBy = currentUser.Username;

        await db.SaveChangesAsync(ct);

        return new UnlockUserResponse(user.Id, user.IsLocked, user.FailedLoginAttempts);
    }
}
