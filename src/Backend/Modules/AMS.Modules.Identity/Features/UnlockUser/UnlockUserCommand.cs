using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.UnlockUser;

/// <summary>
/// Unlock an account and clear its failure count. Catalogue: Create and maintain users, Account lockout.
/// </summary>
public sealed record UnlockUserCommand(
    int UserId) : ICommand<UnlockUserResponse>;
