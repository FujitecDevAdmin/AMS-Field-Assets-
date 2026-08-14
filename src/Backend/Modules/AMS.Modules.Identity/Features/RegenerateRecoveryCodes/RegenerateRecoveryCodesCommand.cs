using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.RegenerateRecoveryCodes;

/// <summary>
/// Replace every recovery code with a fresh set. Catalogue: Multi-factor authentication.
/// </summary>
public sealed record RegenerateRecoveryCodesCommand(
    int UserId,
    string Code) : ICommand<RegenerateRecoveryCodesResponse>;
