namespace AMS.SharedKernel.Abstractions;

/// <summary>
/// Who is asking, and how far they can see.
/// </summary>
/// <remarks>
/// Branch scoping is applied per request inside query handlers, never as a
/// global EF query filter — the schema appendix is explicit that a
/// model-level filter reading request state behaves differently in background
/// jobs, where there is no caller at all.
/// </remarks>
public interface ICurrentUser
{
    int Id { get; }

    string Username { get; }

    /// <summary>
    /// The <c>Organization.Employee</c> this login belongs to, or null.
    /// </summary>
    /// <remarks>
    /// Resolved once at authentication from <c>Identity.User.EmployeeId</c> and
    /// carried as a claim, because every "my ..." screen needs it — my
    /// application access, my assets, my tickets, my approvals — and none of
    /// those modules may reference Identity to look it up (01 §2 rule 2).
    ///
    /// Null is normal: a service account or an administrator who is not in the
    /// employee directory has a login and no employee record. A screen that
    /// shows somebody their own things must say so rather than showing an
    /// empty list as though they simply had none.
    /// </remarks>
    int? EmployeeId { get; }

    /// <summary>Head office. When true, <see cref="BranchIds"/> is not consulted.</summary>
    bool HasAllBranches { get; }

    IReadOnlySet<int> BranchIds { get; }

    /// <summary>
    /// Capability names, already resolved to the effective set (role union
    /// minus per-user denies). Never a role name — docs/01 §2 rule 6.
    /// </summary>
    IReadOnlySet<string> Capabilities { get; }
}
