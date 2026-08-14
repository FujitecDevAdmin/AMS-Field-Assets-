namespace AMS.Modules.Identity.Authentication;

/// <summary>
/// Carries a half-finished sign-in between the password step and the MFA step.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately stateless: the token is protected and time-limited, not a row.
/// The schema has no table for a pending sign-in and does not need one - a
/// challenge is worthless after a minute or two, and a table of them is a
/// table somebody has to clean up.
/// </para>
/// <para>
/// The token proves only that the password step passed for this user. It is
/// not a session and grants nothing on its own.
/// </para>
/// </remarks>
public interface IMfaChallengeTokens
{
    /// <summary>Issues a short-lived token for a user who has passed the password step.</summary>
    string Issue(int userId);

    /// <summary>
    /// Returns the user id when the token is valid and unexpired, otherwise null.
    /// A tampered or stale token is simply invalid; it is never an exception,
    /// because a caller supplying one is an expected event.
    /// </summary>
    int? Validate(string token);
}
