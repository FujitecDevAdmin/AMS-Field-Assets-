namespace AMS.Modules.Verification.Features.CloseVerificationCycle;

/// <summary>
/// The cycle, closed, and what it found.
/// </summary>
/// <param name="Id">The cycle.</param>
/// <param name="VerifiedCount">How many rows were recorded.</param>
/// <param name="ExceptionCount">How many of those were not Good.</param>
/// <param name="ClosedOnUtc">When it was closed.</param>
public sealed record CloseVerificationCycleResponse(
    int Id,
    int VerifiedCount,
    int ExceptionCount,
    DateTime ClosedOnUtc);
