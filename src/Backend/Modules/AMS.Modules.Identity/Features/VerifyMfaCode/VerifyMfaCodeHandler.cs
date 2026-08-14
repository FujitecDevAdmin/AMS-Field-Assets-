using AMS.Modules.Identity.Authentication;
using AMS.Modules.Identity.Persistence;
using AMS.Modules.Identity.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.VerifyMfaCode;

/// <summary>
/// Second step of sign-in: an authenticator code, or a single-use recovery
/// code. Catalogue: Multi-factor authentication.
/// </summary>
/// <remarks>
/// A recovery code is spent by being marked used in the same transaction that
/// accepts it. "Single use" that depends on the application remembering to
/// write the row is not single use.
/// </remarks>
public sealed class VerifyMfaCodeHandler(
    IdentityDbContext db,
    IMfaChallengeTokens challenges,
    ITotpCodes totp,
    ISecretProtector secrets,
    EffectiveAccess effectiveAccess,
    IAccessTokens accessTokens,
    IPasswordHasher hasher,
    IClock clock) : IRequestHandler<VerifyMfaCodeCommand, VerifyMfaCodeResponse>
{
    private static Error InvalidCode =>
        Error.Forbidden("Mfa.Invalid", "That code is not valid. Try again, or use a recovery code.");

    public async Task<Result<VerifyMfaCodeResponse>> HandleAsync(
        VerifyMfaCodeCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = challenges.Validate(request.MfaChallengeToken);
        if (userId is null)
        {
            // Expired or tampered. Start again rather than explain.
            return Error.Forbidden("Mfa.ChallengeExpired", "That sign-in has expired. Please sign in again.");
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId.Value, ct);
        if (user is null || !user.IsActive || user.IsLocked || !user.MfaEnabled)
        {
            return InvalidCode;
        }

        var usedRecoveryCode = false;

        if (user.MfaSecretEncrypted is not null
            && totp.Verify(secrets.Unprotect(user.MfaSecretEncrypted), request.Code))
        {
            // Authenticator code accepted.
        }
        else
        {
            var spent = await SpendRecoveryCodeAsync(user.Id, request.Code, ct);
            if (!spent)
            {
                return InvalidCode;
            }

            usedRecoveryCode = true;
        }

        user.FailedLoginAttempts = 0;
        user.LastLoginOnUtc = clock.UtcNow;
        user.ModifiedOnUtc = clock.UtcNow;

        await db.SaveChangesAsync(ct);

        var remaining = await db.UserRecoveryCodes
            .CountAsync(c => c.UserId == user.Id && c.UsedOnUtc == null, ct);

        // The second factor passed, so the session begins here.
        var access = await effectiveAccess.ResolveAsync(user.Id, user.HasAllBranches, ct);
        var token = accessTokens.Issue(new AccessTokenSubject(
            user.Id, user.Username, user.EmployeeId,
            access.HasAllBranches, access.BranchIds, access.Capabilities));

        return new VerifyMfaCodeResponse(
            user.Id,
            user.Username,
            user.DisplayName,
            user.MustChangePassword,
            usedRecoveryCode,
            remaining,
            token.Token,
            token.ExpiresOnUtc);
    }

    private async Task<bool> SpendRecoveryCodeAsync(int userId, string code, CancellationToken ct)
    {
        // Codes are hashed, so they cannot be looked up by value - each unused
        // code is compared in turn. IX_UserRecoveryCode_UserUnused (R2-15)
        // keeps that list short.
        var unused = await db.UserRecoveryCodes
            .Where(c => c.UserId == userId && c.UsedOnUtc == null)
            .ToListAsync(ct);

        var match = unused.FirstOrDefault(c => hasher.Verify(code, c.CodeHash));
        if (match is null)
        {
            return false;
        }

        match.UsedOnUtc = clock.UtcNow;
        return true;
    }
}
