using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.UpdateBranch;

/// <summary>
/// Edit a branch, move it between regions, or retire it.
/// </summary>
public sealed record UpdateBranchCommand(
    int Id,
    string BranchCode,
    string BranchName,
    int? RegionId,
    string TimeZoneId,
    bool IsHeadOffice,
    bool IsActive) : ICommand<UpdateBranchResponse>;
