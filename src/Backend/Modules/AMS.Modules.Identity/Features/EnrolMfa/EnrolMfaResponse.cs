namespace AMS.Modules.Identity.Features.EnrolMfa;

/// <summary>
/// The secret to enrol with. Returned ONCE and never readable again.
/// </summary>
/// <param name="Secret">Base32, for typing in by hand when a camera will not cooperate.</param>
/// <param name="OtpAuthUri">otpauth:// URI for the QR code.</param>
public sealed record EnrolMfaResponse(
    string Secret,
    string OtpAuthUri);
