using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.ChangeMyPassword;

/// <summary>
/// Set my own password. Catalogue: Change my own password, and the second half of Forced password change.
/// </summary>
public sealed record ChangeMyPasswordCommand(
    int UserId,
    string CurrentPassword,
    string NewPassword) : ICommand<ChangeMyPasswordResponse>;
