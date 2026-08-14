namespace AMS.Infrastructure.Security;

/// <summary>
/// The claim types this application issues and reads.
/// </summary>
/// <remarks>
/// Named constants because a claim type is a wire contract between the token
/// this application issues and the middleware that reads it back. A typo in a
/// string literal on one side produces a caller with no capabilities and no
/// error — the screen simply refuses everything.
/// </remarks>
public static class AmsClaims
{
    /// <summary>The <c>Identity.User</c> id.</summary>
    public const string UserId = "ams:uid";

    /// <summary>The <c>Organization.Employee</c> this login belongs to, if any.</summary>
    public const string EmployeeId = "ams:eid";

    /// <summary>Present and "true" for head office: every branch, no branch list.</summary>
    public const string AllBranches = "ams:all-branches";

    /// <summary>One per branch the caller may see. Absent when they see all.</summary>
    public const string Branch = "ams:branch";

    /// <summary>
    /// One per effective capability — the union of the caller's roles' grants,
    /// minus any per-user deny, resolved once at sign-in.
    /// </summary>
    public const string Capability = "ams:cap";
}
