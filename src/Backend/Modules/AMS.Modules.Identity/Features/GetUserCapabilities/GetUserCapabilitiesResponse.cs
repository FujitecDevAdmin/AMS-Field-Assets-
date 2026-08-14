namespace AMS.Modules.Identity.Features.GetUserCapabilities;

/// <summary>
/// What the user may actually do, after roles and overrides have been resolved.
/// </summary>
/// <param name="UserId">The user asked about.</param>
/// <param name="Username">For display and for log correlation.</param>
/// <param name="HasAllBranches">
/// Head office. When true, <paramref name="BranchIds"/> is empty and is not
/// consulted.
/// </param>
/// <param name="BranchIds">
/// The branches this user may see. Empty when
/// <paramref name="HasAllBranches"/> is true.
/// </param>
/// <param name="Capabilities">
/// The effective set: the union of every ACTIVE role's grants, plus per-user
/// grants, MINUS every per-user deny. A deny wins.
/// </param>
public sealed record GetUserCapabilitiesResponse(
    int UserId,
    string Username,
    bool HasAllBranches,
    IReadOnlyList<int> BranchIds,
    IReadOnlyList<string> Capabilities);
