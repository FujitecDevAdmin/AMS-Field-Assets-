using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.LockUser;

/// <summary>
/// Lock an account. Catalogue: Create and maintain users.
/// </summary>
public sealed record LockUserCommand(
    int UserId,
    string? Reason) : ICommand<LockUserResponse>;
