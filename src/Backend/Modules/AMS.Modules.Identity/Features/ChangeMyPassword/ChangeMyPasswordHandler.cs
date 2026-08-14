using AMS.Modules.Identity.Persistence;
using AMS.Modules.Identity.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.ChangeMyPassword;

/// <summary>
/// Catalogue: Change my own password, and the half of Forced password change
/// that clears the flag.
/// </summary>
/// <remarks>
/// The current password is re-checked even though the caller is already signed
/// in. A session left open on an unlocked machine should not be enough to take
/// the account.
/// </remarks>
public sealed class ChangeMyPasswordHandler(
    IdentityDbContext db,
    IPasswordHasher hasher,
    IClock clock) : IRequestHandler<ChangeMyPasswordCommand, ChangeMyPasswordResponse>
{
    public async Task<Result<ChangeMyPasswordResponse>> HandleAsync(
        ChangeMyPasswordCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Error.NotFound("User", request.UserId);
        }

        if (!user.IsActive || user.IsLocked)
        {
            return Error.Forbidden("Password.NotPermitted", "This account cannot change its password.");
        }

        if (!hasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Error.Forbidden("Password.CurrentIncorrect", "Your current password is incorrect.");
        }

        user.PasswordHash = hasher.Hash(request.NewPassword);

        // Whatever forced the change - a new account or an admin reset - is
        // now satisfied.
        user.MustChangePassword = false;
        user.ModifiedOnUtc = clock.UtcNow;
        user.ModifiedBy = user.Username;

        await db.SaveChangesAsync(ct);

        return new ChangeMyPasswordResponse(user.Id, user.MustChangePassword);
    }
}
