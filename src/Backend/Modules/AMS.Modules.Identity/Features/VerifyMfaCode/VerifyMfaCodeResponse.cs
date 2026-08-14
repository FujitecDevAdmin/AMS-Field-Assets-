namespace AMS.Modules.Identity.Features.VerifyMfaCode;

/// <summary>
/// A completed sign-in.
/// </summary>
/// <param name="UserId">The signed-in user.</param>
/// <param name="Username">As stored.</param>
/// <param name="DisplayName">For the application header.</param>
/// <param name="MustChangePassword">Carried through from the password step.</param>
/// <param name="UsedRecoveryCode">True when a recovery code was spent rather than an authenticator code. The client should say so.</param>
/// <param name="RemainingRecoveryCodes">How many are left. Prompts regeneration near zero.</param>
/// <param name="AccessToken">The bearer token. This step is where a session begins.</param>
/// <param name="AccessTokenExpiresOnUtc">When it stops working.</param>
public sealed record VerifyMfaCodeResponse(
    int UserId,
    string Username,
    string DisplayName,
    bool MustChangePassword,
    bool UsedRecoveryCode,
    int RemainingRecoveryCodes,
    string AccessToken,
    DateTime AccessTokenExpiresOnUtc);
