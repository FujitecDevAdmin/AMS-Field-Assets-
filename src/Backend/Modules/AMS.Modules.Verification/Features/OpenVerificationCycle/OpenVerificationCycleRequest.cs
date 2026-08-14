namespace AMS.Modules.Verification.Features.OpenVerificationCycle;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record OpenVerificationCycleRequest(
    string CycleName,
    DateOnly? StartDate,
    DateOnly? EndDate);
