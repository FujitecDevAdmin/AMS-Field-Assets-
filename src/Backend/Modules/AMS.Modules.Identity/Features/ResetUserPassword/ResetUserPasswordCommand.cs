using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.ResetUserPassword;

/// <summary>
/// Set a temporary password the user must then change. Catalogue: Create and maintain users, Forced password change.
/// </summary>
public sealed record ResetUserPasswordCommand(
    int UserId,
    string NewPassword) : ICommand<ResetUserPasswordResponse>;
