using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.VerifyMfaCode;

/// <summary>
/// Complete a sign-in with an authenticator code or a single-use recovery code. Catalogue: Multi-factor authentication.
/// </summary>
public sealed record VerifyMfaCodeCommand(
    string MfaChallengeToken,
    string Code) : ICommand<VerifyMfaCodeResponse>;
