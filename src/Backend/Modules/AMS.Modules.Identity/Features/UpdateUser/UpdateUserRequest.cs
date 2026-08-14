namespace AMS.Modules.Identity.Features.UpdateUser;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateUserRequest(
    string DisplayName,
    string? Email,
    int? EmployeeId,
    bool IsActive,
    bool HasAllBranches,
    string ETag);
