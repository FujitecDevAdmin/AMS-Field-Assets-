namespace AMS.SharedKernel.Abstractions;

/// <summary>
/// Issues the bearer token a completed sign-in returns.
/// </summary>
/// <remarks>
/// <para>
/// An abstraction rather than a JWT type in the Identity module, for the same
/// reason <see cref="IClock"/> is one: the module owns <i>who signed in</i>,
/// and the host owns <i>how a session is represented</i>. A module that built
/// its own JWT would fix the signing key, the lifetime and the algorithm in a
/// place the host cannot configure.
/// </para>
/// <para>
/// The capability set is passed IN, not looked up here. It is the effective set
/// — role grants minus per-user denies — and resolving it is Identity's job.
/// </para>
/// </remarks>
public interface IAccessTokens
{
    /// <summary>Issues a token for a sign-in that has fully completed.</summary>
    AccessToken Issue(AccessTokenSubject subject);
}

/// <summary>Who the token is for, and what it says they may do.</summary>
/// <param name="UserId">The <c>Identity.User</c> id.</param>
/// <param name="Username">For display and for the audit trail's PerformedBy.</param>
/// <param name="EmployeeId">
/// The employee this login belongs to, or null. Null is normal — a service
/// account or an administrator outside the directory has a login and no
/// employee record — and every "my ..." screen needs it.
/// </param>
/// <param name="HasAllBranches">Head office. When true the branch list is not consulted.</param>
/// <param name="BranchIds">The branches this login may see. Empty when it sees all.</param>
/// <param name="Capabilities">
/// The effective capability set, already resolved: the union of the roles'
/// grants minus any per-user deny, because a deny must win.
/// </param>
public sealed record AccessTokenSubject(
    int UserId,
    string Username,
    int? EmployeeId,
    bool HasAllBranches,
    IReadOnlyCollection<int> BranchIds,
    IReadOnlyCollection<string> Capabilities);

/// <summary>The issued token and when it stops working.</summary>
/// <param name="Token">The bearer value.</param>
/// <param name="ExpiresOnUtc">
/// When it expires. Returned so the client can refresh before a user loses a
/// half-filled form, rather than discovering it on the next 401.
/// </param>
public sealed record AccessToken(string Token, DateTime ExpiresOnUtc);
