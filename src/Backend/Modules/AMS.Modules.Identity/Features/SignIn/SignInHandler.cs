using AMS.Modules.Identity.Authentication;
using AMS.Modules.Identity.Persistence;
using AMS.Modules.Identity.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.SignIn;

/// <summary>
/// Password step of sign-in. Catalogue: Sign in, Forced password change,
/// Account lockout.
/// </summary>
/// <remarks>
/// <para>
/// Every failure returns the SAME error. "No such user" and "wrong password"
/// told apart is a way to enumerate usernames, and "your account is locked"
/// confirms the username exists. The user sees one message; the log records
/// which it was.
/// </para>
/// <para>
/// The failed-attempt counter is written even on the failure path, so a
/// rollback would hand an attacker unlimited guesses.
/// </para>
/// </remarks>
public sealed class SignInHandler(
    IdentityDbContext db,
    IPasswordHasher hasher,
    IMfaChallengeTokens challenges,
    EffectiveAccess effectiveAccess,
    IAccessTokens accessTokens,
    IClock clock) : IRequestHandler<SignInCommand, SignInResponse>
{
    /// <summary>The one message every failure returns.</summary>
    private static Error InvalidCredentials =>
        Error.Forbidden("SignIn.Invalid", "The username or password is incorrect.");

    public async Task<Result<SignInResponse>> HandleAsync(SignInCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == request.Username, ct);

        if (user is null || !user.IsActive || user.IsLocked)
        {
            return InvalidCredentials;
        }

        if (!hasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= LockoutPolicy.MaxFailedAttempts)
            {
                // Stays locked until an administrator unlocks it. No timer.
                user.IsLocked = true;
            }

            user.ModifiedOnUtc = clock.UtcNow;
            await db.SaveChangesAsync(ct);

            return InvalidCredentials;
        }

        user.FailedLoginAttempts = 0;
        user.LastLoginOnUtc = clock.UtcNow;
        user.ModifiedOnUtc = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        // Enrolled users are not signed in yet. The caller must complete
        // VerifyMfaCode; this response is a challenge, not a session.
        var mfaRequired = user.MfaEnabled;

        if (mfaRequired)
        {
            return new SignInResponse(
                user.Id,
                user.Username,
                user.DisplayName,
                user.MustChangePassword,
                MfaRequired: true,
                challenges.Issue(user.Id),
                AccessToken: null,
                AccessTokenExpiresOnUtc: null);
        }

        // No second factor, so this IS the session. The capability set is
        // resolved once, here, and travels in the token - which is what keeps
        // [Identity] out of the path of every request in every other module.
        var access = await effectiveAccess.ResolveAsync(user.Id, user.HasAllBranches, ct);
        var token = accessTokens.Issue(new AccessTokenSubject(
            user.Id, user.Username, user.EmployeeId,
            access.HasAllBranches, access.BranchIds, access.Capabilities));

        return new SignInResponse(
            user.Id,
            user.Username,
            user.DisplayName,
            user.MustChangePassword,
            MfaRequired: false,
            MfaChallengeToken: null,
            token.Token,
            token.ExpiresOnUtc);
    }
}
