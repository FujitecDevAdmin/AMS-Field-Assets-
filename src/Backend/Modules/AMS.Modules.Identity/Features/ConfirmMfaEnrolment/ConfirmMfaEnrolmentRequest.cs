namespace AMS.Modules.Identity.Features.ConfirmMfaEnrolment;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record ConfirmMfaEnrolmentRequest(
    string Code);
