namespace AMS.Modules.Identity.Features.GetMyProfile;

/// <summary>
/// What the signed-in user may see about themselves.
/// </summary>
/// <param name="UserId">Themselves.</param>
/// <param name="Username">As stored.</param>
/// <param name="DisplayName">For the application header.</param>
/// <param name="Email">May be null; not every user has one.</param>
/// <param name="MustChangePassword">True until they set their own password.</param>
/// <param name="MfaEnabled">True once enrolment is confirmed, not merely started.</param>
/// <param name="RemainingRecoveryCodes">Unused codes. The profile screen nags near zero.</param>
/// <param name="HasAllBranches">Head office.</param>
/// <param name="BranchIds">Empty when HasAllBranches is true.</param>
public sealed record GetMyProfileResponse(
    int UserId,
    string Username,
    string DisplayName,
    string? Email,
    bool MustChangePassword,
    bool MfaEnabled,
    int RemainingRecoveryCodes,
    bool HasAllBranches,
    IReadOnlyList<int> BranchIds);
