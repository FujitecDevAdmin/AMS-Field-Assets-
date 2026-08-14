namespace AMS.Modules.Identity.Features.SearchUsers;

/// <summary>
/// One page of users, and how many match in total.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">
/// Rows matching the filter, ignoring paging. The grid needs it to size the
/// scrollbar (docs/04 §3).
/// </param>
public sealed record SearchUsersResponse(
    IReadOnlyList<SearchUsersResponse.Row> Rows,
    int TotalCount)
{
    /// <summary>
    /// One line of the Users grid.
    /// </summary>
    /// <remarks>
    /// Nested rather than its own file on purpose: a slice folder holds seven
    /// files with fixed suffixes (01 §3), and <c>SearchUsersRow.cs</c> is none
    /// of them. Nesting keeps one public top-level type per file as well.
    /// </remarks>
    /// <param name="Id">The user.</param>
    /// <param name="Username">As stored.</param>
    /// <param name="DisplayName">Shown in the grid.</param>
    /// <param name="Email">May be null.</param>
    /// <param name="IsActive">Deactivated users still appear, greyed.</param>
    /// <param name="IsLocked">Locked accounts are what an administrator opens this screen for.</param>
    /// <param name="MfaEnabled">Enrolment state, for the compliance view.</param>
    /// <param name="LastLoginOnUtc">Null when the account has never been used.</param>
    public sealed record Row(
        int Id,
        string Username,
        string DisplayName,
        string? Email,
        bool IsActive,
        bool IsLocked,
        bool MfaEnabled,
        DateTime? LastLoginOnUtc);
}
