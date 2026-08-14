using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.CreateUser;

/// <summary>
/// Create a login. Immutable record; the handler is the only place the work
/// happens (docs/01 §3).
/// </summary>
public sealed record CreateUserCommand(
    string Username,
    string DisplayName,
    string PasswordHash,
    string? Email,
    int? EmployeeId,
    bool HasAllBranches,
    IReadOnlyList<int> BranchIds,
    int? PrimaryBranchId) : ICommand<CreateUserResponse>;
