namespace AMS.Modules.Organization.Features.CreateBranch;

/// <summary>
/// The new branch.
/// </summary>
/// <param name="Id">The new branch.</param>
/// <param name="BranchCode">Unique, upper-cased.</param>
/// <param name="BranchName">As stored, trimmed.</param>
/// <param name="Latitude">Branch latitude in decimal degrees.</param>
/// <param name="Longitude">Branch longitude in decimal degrees.</param>
/// <param name="IsHeadOffice">At most one branch in the whole system has this.</param>
public sealed record CreateBranchResponse(
    int Id,
    string BranchCode,
    string BranchName,
    decimal? Latitude,
    decimal? Longitude,
    bool IsHeadOffice);
