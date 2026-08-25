using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.CreateBranch;

/// <summary>
/// Open a branch. Catalogue: Branches and branches, Put a branch in a region, Branch time zone.
/// </summary>
public sealed record CreateBranchCommand(
    string BranchCode,
    string BranchName,
    int? RegionId,
    decimal? Latitude,
    decimal? Longitude,
    string TimeZoneId,
    bool IsHeadOffice) : ICommand<CreateBranchResponse>;
