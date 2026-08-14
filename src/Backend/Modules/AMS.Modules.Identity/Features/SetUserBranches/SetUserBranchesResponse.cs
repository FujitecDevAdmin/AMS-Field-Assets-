namespace AMS.Modules.Identity.Features.SetUserBranches;

/// <summary>
/// The branches the user now sees.
/// </summary>
/// <param name="UserId">The user changed.</param>
/// <param name="BranchIds">The complete set afterwards.</param>
/// <param name="PrimaryBranchId">At most one; UX_UserBranch_OnePrimary is what enforces that.</param>
public sealed record SetUserBranchesResponse(
    int UserId,
    IReadOnlyList<int> BranchIds,
    int? PrimaryBranchId);
