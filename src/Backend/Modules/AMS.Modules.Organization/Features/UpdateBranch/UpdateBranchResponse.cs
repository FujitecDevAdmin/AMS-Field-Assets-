namespace AMS.Modules.Organization.Features.UpdateBranch;

/// <summary>
/// The updated branch.
/// </summary>
/// <param name="Id">The branch edited.</param>
/// <param name="BranchCode">Unique, upper-cased.</param>
/// <param name="IsHeadOffice">At most one across the system.</param>
/// <param name="IsActive">Retiring is deactivation; assets and employees still point here.</param>
public sealed record UpdateBranchResponse(
    int Id,
    string BranchCode,
    bool IsHeadOffice,
    bool IsActive);
