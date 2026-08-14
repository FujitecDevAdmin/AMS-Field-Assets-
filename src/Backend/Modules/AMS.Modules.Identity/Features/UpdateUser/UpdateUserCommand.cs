using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.UpdateUser;

/// <summary>
/// Edit a user. Catalogue: Create and maintain users.
/// </summary>
public sealed record UpdateUserCommand(
    int UserId,
    string DisplayName,
    string? Email,
    int? EmployeeId,
    bool IsActive,
    bool HasAllBranches,
    string ETag) : ICommand<UpdateUserResponse>;
