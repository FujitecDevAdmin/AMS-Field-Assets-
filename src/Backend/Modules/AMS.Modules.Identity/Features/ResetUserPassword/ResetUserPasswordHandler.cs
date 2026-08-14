using AMS.Modules.Identity.Persistence;
using AMS.Modules.Identity.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.ResetUserPassword;

/// <summary>
/// An administrator sets a temporary password. Catalogue: Create and maintain
/// users, Forced password change.
/// </summary>
/// <remarks>
/// <c>MustChangePassword</c> is forced true: somebody other than the account
/// holder has just seen this password, so it cannot be allowed to stay.
/// </remarks>
public sealed class ResetUserPasswordHandler(
    IdentityDbContext db,
    IPasswordHasher hasher,
    IClock clock,
    ICurrentUser currentUser) : IRequestHandler<ResetUserPasswordCommand, ResetUserPasswordResponse>
{
    public async Task<Result<ResetUserPasswordResponse>> HandleAsync(
        ResetUserPasswordCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Error.NotFound("User", request.UserId);
        }

        user.PasswordHash = hasher.Hash(request.NewPassword);
        user.MustChangePassword = true;

        // A reset is also how an administrator helps somebody who locked
        // themselves out, so the count goes with it.
        user.FailedLoginAttempts = 0;
        user.ModifiedOnUtc = clock.UtcNow;
        user.ModifiedBy = currentUser.Username;

        await db.SaveChangesAsync(ct);

        return new ResetUserPasswordResponse(user.Id, user.MustChangePassword);
    }
}
