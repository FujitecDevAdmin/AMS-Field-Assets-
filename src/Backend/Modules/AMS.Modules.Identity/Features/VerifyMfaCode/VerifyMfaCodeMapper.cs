namespace AMS.Modules.Identity.Features.VerifyMfaCode;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class VerifyMfaCodeMapper
{
    public static VerifyMfaCodeCommand ToCommand(VerifyMfaCodeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new VerifyMfaCodeCommand(
            request.MfaChallengeToken,
            request.Code.Trim());
    }
}
