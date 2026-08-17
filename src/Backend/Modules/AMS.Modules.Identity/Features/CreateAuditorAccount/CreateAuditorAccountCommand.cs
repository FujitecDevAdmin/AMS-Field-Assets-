using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.CreateAuditorAccount;

public sealed record CreateAuditorAccountCommand(
    string Username,
    string DisplayName,
    string PasswordHash,
    string? Email,
    int? EmployeeId,
    bool HasAllBranches,
    IReadOnlyList<int> BranchIds,
    int? PrimaryBranchId,
    bool RequireMfa) : ICommand<CreateAuditorAccountResponse>;
