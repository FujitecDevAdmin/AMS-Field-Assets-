namespace AMS.Modules.Identity.Features.CreateAuditorAccount;

public sealed record CreateAuditorAccountRequest(
    string Username,
    string DisplayName,
    string Password,
    string? Email,
    int? EmployeeId,
    bool HasAllBranches,
    IReadOnlyList<int>? BranchIds,
    int? PrimaryBranchId,
    bool RequireMfa);
