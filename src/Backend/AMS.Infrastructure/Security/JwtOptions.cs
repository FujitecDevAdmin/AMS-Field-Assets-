using System.ComponentModel.DataAnnotations;

namespace AMS.Infrastructure.Security;

/// <summary>
/// How this deployment signs and validates its bearer tokens.
/// </summary>
/// <remarks>
/// Validated on start-up, not on first use. A deployment missing its signing
/// key should fail to boot with a message naming the setting, rather than
/// accept sign-ins for an hour and then refuse every request with a 401 nobody
/// can explain.
/// </remarks>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Who issued the token. Checked on the way back in.</summary>
    [Required]
    public string Issuer { get; set; } = "ams";

    /// <summary>Who the token is for. Checked on the way back in.</summary>
    [Required]
    public string Audience { get; set; } = "ams";

    /// <summary>
    /// The signing key. Never in source control — user-secrets in development,
    /// the platform's secret store in production.
    /// </summary>
    /// <remarks>
    /// 32 bytes minimum because HMAC-SHA256 with a shorter key is weaker than
    /// it looks, and the failure is silent.
    /// </remarks>
    [Required]
    [MinLength(32, ErrorMessage = "The JWT signing key must be at least 32 characters.")]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// How long a session lasts.
    /// </summary>
    /// <remarks>
    /// Eight hours: a working day, so an asset administrator is not signed out
    /// mid-stocktake. It is also how long a capability change takes to reach
    /// somebody, because the claims are resolved at sign-in.
    /// </remarks>
    [Range(5, 24 * 60)]
    public int LifetimeMinutes { get; set; } = 480;
}
