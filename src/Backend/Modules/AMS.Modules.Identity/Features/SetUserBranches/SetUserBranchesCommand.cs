using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.SetUserBranches;

/// <summary>
/// Replace the branches a user sees. Catalogue: Set which branches a user sees.
/// </summary>
public sealed record SetUserBranchesCommand(
    int UserId,
    IReadOnlyList<int> BranchIds,
    int? PrimaryBranchId) : ICommand<SetUserBranchesResponse>;
