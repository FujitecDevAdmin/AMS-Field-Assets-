namespace AMS.Modules.Identity.Features.ConfirmMfaEnrolment;

/// <summary>
/// Enrolment confirmed, with the recovery codes.
/// </summary>
/// <param name="MfaEnabled">True. Sign-in will challenge from now on.</param>
/// <param name="RecoveryCodes">Shown ONCE. Only hashes are stored, so nobody - including an administrator - can ever read them back.</param>
public sealed record ConfirmMfaEnrolmentResponse(
    bool MfaEnabled,
    IReadOnlyList<string> RecoveryCodes);
