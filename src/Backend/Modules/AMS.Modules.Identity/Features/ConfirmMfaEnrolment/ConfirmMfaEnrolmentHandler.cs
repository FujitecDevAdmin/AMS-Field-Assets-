using AMS.Modules.Identity.Authentication;
using AMS.Modules.Identity.Domain;
using AMS.Modules.Identity.Persistence;
using AMS.Modules.Identity.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.ConfirmMfaEnrolment;

/// <summary>
/// Proves the authenticator works, switches MFA on and issues recovery codes.
/// Catalogue: "verify a code at sign-in, keep single-use recovery codes".
/// </summary>
/// <remarks>
/// Switching MFA on and creating the codes happen in one <c>SaveChanges</c>.
/// A user whose MFA came on but whose recovery codes did not is one lost phone
/// away from an account nobody can reach.
/// </remarks>
public sealed class ConfirmMfaEnrolmentHandler(
    IdentityDbContext db,
    ITotpCodes totp,
    ISecretProtector secrets,
    IPasswordHasher hasher,
    IClock clock) : IRequestHandler<ConfirmMfaEnrolmentCommand, ConfirmMfaEnrolmentResponse>
{
    public async Task<Result<ConfirmMfaEnrolmentResponse>> HandleAsync(
        ConfirmMfaEnrolmentCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Error.NotFound("User", request.UserId);
        }

        if (user.MfaSecretEncrypted is null)
        {
            return Error.Forbidden("Mfa.NotStarted", "Start enrolment before confirming it.");
        }

        if (user.MfaEnabled)
        {
            return Error.Conflict("Mfa.AlreadyEnrolled", "Multi-factor authentication is already switched on.");
        }

        if (!totp.Verify(secrets.Unprotect(user.MfaSecretEncrypted), request.Code))
        {
            return Error.Forbidden(
                "Mfa.Invalid",
                "That code is not valid. Check the time on your phone and try the next one.");
        }

        var codes = RecoveryCodes.CreateSet();

        // Any codes from a previous enrolment are meaningless against the new
        // secret and must not be left usable.
        var stale = await db.UserRecoveryCodes.Where(c => c.UserId == user.Id).ToListAsync(ct);
        db.UserRecoveryCodes.RemoveRange(stale);

        foreach (var code in codes)
        {
            db.UserRecoveryCodes.Add(new UserRecoveryCode
            {
                UserId = user.Id,
                CodeHash = hasher.Hash(code),
                CreatedOnUtc = clock.UtcNow,
            });
        }

        user.MfaEnabled = true;
        user.MfaEnrolledOnUtc = clock.UtcNow;
        user.MfaEnrollmentRequired = false;
        user.ModifiedOnUtc = clock.UtcNow;
        user.ModifiedBy = user.Username;

        await db.SaveChangesAsync(ct);

        // The only time these are readable. Only hashes were stored.
        return new ConfirmMfaEnrolmentResponse(true, codes);
    }
}
