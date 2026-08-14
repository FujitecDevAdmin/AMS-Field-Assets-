namespace AMS.Modules.Verification.Features.SearchVerificationCycles;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchVerificationCyclesRequest(
    bool? ActiveOnly);
