using AMS.Modules.Identity.Authentication;
using AMS.Modules.Identity.Domain;
using AMS.Modules.Identity.Persistence;
using AMS.Modules.Identity.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.RegenerateRecoveryCodes;

/// <summary>
/// Replaces every recovery code with a fresh set. Catalogue: "keep single-use
/// recovery codes".
/// </summary>
/// <remarks>
/// An authenticator code is required first. Regenerating from a session
/// somebody walked away from would replace the real owner's codes with a set
/// only the attacker has seen, and the owner would find out the next time they
/// lost their phone.
/// </remarks>
public sealed class RegenerateRecoveryCodesHandler(
    IdentityDbContext db,
    ITotpCodes totp,
    ISecretProtector secrets,
    IPasswordHasher hasher,
    IClock clock) : IRequestHandler<RegenerateRecoveryCodesCommand, RegenerateRecoveryCodesResponse>
{
    public async Task<Result<RegenerateRecoveryCodesResponse>> HandleAsync(
        RegenerateRecoveryCodesCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Error.NotFound("User", request.UserId);
        }

        if (!user.MfaEnabled || user.MfaSecretEncrypted is null)
        {
            return Error.Forbidden("Mfa.NotEnrolled", "Multi-factor authentication is not switched on.");
        }

        if (!totp.Verify(secrets.Unprotect(user.MfaSecretEncrypted), request.Code))
        {
            return Error.Forbidden("Mfa.Invalid", "That code is not valid.");
        }

        // Every previous code, used or not, stops working. That is the point:
        // a set you are unsure about is a set you replace entirely.
        var previous = await db.UserRecoveryCodes.Where(c => c.UserId == user.Id).ToListAsync(ct);
        db.UserRecoveryCodes.RemoveRange(previous);

        var codes = RecoveryCodes.CreateSet();

        foreach (var code in codes)
        {
            db.UserRecoveryCodes.Add(new UserRecoveryCode
            {
                UserId = user.Id,
                CodeHash = hasher.Hash(code),
                CreatedOnUtc = clock.UtcNow,
            });
        }

        user.ModifiedOnUtc = clock.UtcNow;
        user.ModifiedBy = user.Username;

        await db.SaveChangesAsync(ct);

        return new RegenerateRecoveryCodesResponse(codes);
    }
}
