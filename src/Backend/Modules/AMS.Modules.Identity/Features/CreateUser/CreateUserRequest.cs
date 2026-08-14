namespace AMS.Modules.Identity.Features.CreateUser;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction
/// (docs/01 §3).
/// </summary>
/// <remarks>
/// The plain password arrives here and is hashed in the handler. It is never
/// logged, never echoed back, and never reaches the domain entity as clear
/// text.
/// </remarks>
public sealed record CreateUserRequest(
    string Username,
    string DisplayName,
    string Password,
    string? Email,
    int? EmployeeId,
    bool HasAllBranches,
    IReadOnlyList<int>? BranchIds,
    int? PrimaryBranchId);
