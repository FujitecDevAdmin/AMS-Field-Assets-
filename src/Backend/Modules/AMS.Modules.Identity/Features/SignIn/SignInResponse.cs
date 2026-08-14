namespace AMS.Modules.Identity.Features.SignIn;

/// <summary>
/// The outcome of a sign-in attempt. Never says WHY it failed.
/// </summary>
/// <param name="UserId">The signed-in user.</param>
/// <param name="Username">As stored.</param>
/// <param name="DisplayName">For the application header.</param>
/// <param name="MustChangePassword">True for a new or admin-reset account; the client must route to the password change screen before anything else.</param>
/// <param name="MfaRequired">True when the user is enrolled. The session is NOT usable until VerifyMfaCode succeeds.</param>
/// <param name="MfaChallengeToken">Short-lived token identifying this half-finished sign-in. Null when MFA is not required.</param>
/// <param name="AccessToken">
/// The bearer token, when this sign-in is COMPLETE. Null whenever
/// <paramref name="MfaRequired"/> is true: an enrolled user is not signed in
/// until VerifyMfaCode succeeds, and issuing a usable token beside a
/// challenge would make the second factor optional.
/// </param>
/// <param name="AccessTokenExpiresOnUtc">When it stops working, so the client can refresh before a form is lost.</param>
public sealed record SignInResponse(
    int UserId,
    string Username,
    string DisplayName,
    bool MustChangePassword,
    bool MfaRequired,
    string? MfaChallengeToken,
    string? AccessToken,
    DateTime? AccessTokenExpiresOnUtc);
