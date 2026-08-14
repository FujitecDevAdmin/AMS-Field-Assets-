namespace AMS.Modules.Identity.Features.EnrolMfa;

/// <summary>
/// The HTTP wire shape. Empty: this slice takes everything it needs from the
/// route and the caller's identity (docs/01 §3).
/// </summary>
public sealed record EnrolMfaRequest;
