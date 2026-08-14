using System.Globalization;
using AMS.SharedKernel.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace AMS.Modules.Identity.Authentication;

/// <summary>
/// Protected, time-limited challenge tokens. See <see cref="IMfaChallengeTokens"/>.
/// </summary>
public sealed class MfaChallengeTokens(IDataProtectionProvider provider, IClock clock) : IMfaChallengeTokens
{
    /// <summary>
    /// Purpose string. Changing it invalidates every outstanding challenge,
    /// which is harmless: the worst case is somebody signs in again.
    /// </summary>
    private const string Purpose = "AMS.Identity.MfaChallenge";

    /// <summary>
    /// Long enough to read a code off a phone, short enough that a token
    /// captured from a log is useless by the time anybody finds it.
    /// </summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly ITimeLimitedDataProtector _protector =
        provider.CreateProtector(Purpose).ToTimeLimitedDataProtector();

    public string Issue(int userId) =>
        _protector.Protect(userId.ToString(CultureInfo.InvariantCulture), clock.UtcNow.Add(Lifetime));

    public int? Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var payload = _protector.Unprotect(token);
            return int.TryParse(payload, CultureInfo.InvariantCulture, out var userId) ? userId : null;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Tampered, expired, or protected with a key this instance no
            // longer holds. All three mean the same thing to the caller.
            return null;
        }
    }
}
