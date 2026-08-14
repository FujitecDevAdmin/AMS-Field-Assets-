namespace AMS.Modules.Identity.Features.GetUser;

/// <summary>
/// Everything the Users screen shows for one person.
/// </summary>
/// <param name="Id">See the handler.</param>
/// <param name="Username">See the handler.</param>
/// <param name="DisplayName">See the handler.</param>
/// <param name="Email">See the handler.</param>
/// <param name="EmployeeId">See the handler.</param>
/// <param name="IsActive">See the handler.</param>
/// <param name="IsLocked">See the handler.</param>
/// <param name="MustChangePassword">See the handler.</param>
/// <param name="MfaEnabled">See the handler.</param>
/// <param name="HasAllBranches">See the handler.</param>
/// <param name="RoleIds">Roles held, whether or not the role itself is active.</param>
/// <param name="BranchIds">Empty when HasAllBranches is true.</param>
/// <param name="PrimaryBranchId">At most one, enforced by UX_UserBranch_OnePrimary.</param>
/// <param name="ETag">RowVersion, base64. Carried back on the next edit; a mismatch is a 412.</param>
public sealed record GetUserResponse(
    int Id,
    string Username,
    string DisplayName,
    string? Email,
    int? EmployeeId,
    bool IsActive,
    bool IsLocked,
    bool MustChangePassword,
    bool MfaEnabled,
    bool HasAllBranches,
    IReadOnlyList<int> RoleIds,
    IReadOnlyList<int> BranchIds,
    int? PrimaryBranchId,
    string ETag);
