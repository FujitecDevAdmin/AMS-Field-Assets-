using System.Net;
using AMS.Modules.Identity.Authentication;
using AMS.Modules.Identity.Persistence;
using AMS.Modules.Identity.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.EnrolMfa;

/// <summary>
/// Begins MFA enrolment. Catalogue: "Enrol with an authenticator app".
/// </summary>
/// <remarks>
/// <para>
/// The secret is stored immediately but <c>MfaEnabled</c> stays FALSE until
/// <c>ConfirmMfaEnrolment</c> proves the app actually works. Turning MFA on
/// here would lock out anybody whose camera failed halfway through.
/// </para>
/// <para>
/// Starting enrolment again simply replaces the unconfirmed secret, which is
/// what somebody who abandoned the first attempt expects.
/// </para>
/// </remarks>
public sealed class EnrolMfaHandler(
    IdentityDbContext db,
    ITotpCodes totp,
    ISecretProtector secrets,
    IClock clock) : IRequestHandler<EnrolMfaCommand, EnrolMfaResponse>
{
    /// <summary>Shown in the authenticator app's list.</summary>
    private const string Issuer = "Fujitec AMS";

    public async Task<Result<EnrolMfaResponse>> HandleAsync(EnrolMfaCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Error.NotFound("User", request.UserId);
        }

        if (user.MfaEnabled)
        {
            // Re-enrolling would silently invalidate the working authenticator
            // and every recovery code with it.
            return Error.Conflict(
                "Mfa.AlreadyEnrolled",
                "Multi-factor authentication is already switched on. Turn it off before enrolling again.");
        }

        var secret = totp.CreateSecret();

        user.MfaSecretEncrypted = secrets.Protect(secret);
        user.MfaEnrolledOnUtc = null;
        user.ModifiedOnUtc = clock.UtcNow;
        user.ModifiedBy = user.Username;

        await db.SaveChangesAsync(ct);

        var label = WebUtility.UrlEncode($"{Issuer}:{user.Username}");
        var issuer = WebUtility.UrlEncode(Issuer);
        var uri = $"otpauth://totp/{label}?secret={secret}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";

        return new EnrolMfaResponse(secret, uri);
    }
}
