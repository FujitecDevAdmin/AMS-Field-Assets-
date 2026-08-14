using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.ConfirmMfaEnrolment;

/// <summary>
/// Prove the authenticator works, turn MFA on, and issue recovery codes. Catalogue: Multi-factor authentication.
/// </summary>
public sealed record ConfirmMfaEnrolmentCommand(
    int UserId,
    string Code) : ICommand<ConfirmMfaEnrolmentResponse>;
