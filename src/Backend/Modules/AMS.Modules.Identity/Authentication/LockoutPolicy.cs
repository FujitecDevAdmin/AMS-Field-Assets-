namespace AMS.Modules.Identity.Authentication;

/// <summary>
/// When repeated failed sign-ins lock an account.
/// </summary>
/// <remarks>
/// <para>
/// Catalogue: "Repeated failed sign-ins lock the account until an
/// administrator unlocks it." The handbook does not say how many, and the
/// schema has no settings table for it, so the number lives here as one named
/// constant rather than as a literal buried in a handler.
/// </para>
/// <para>
/// There is no automatic unlock on a timer. The catalogue is explicit that an
/// administrator unlocks the account, and a lockout that quietly expires is
/// not a lockout - it is a delay.
/// </para>
/// </remarks>
public static class LockoutPolicy
{
    /// <summary>Consecutive failures that lock the account.</summary>
    public const int MaxFailedAttempts = 5;
}
