namespace AMS.Modules.Verification.Features.OpenVerificationCycle;

/// <summary>
/// The cycle, open.
/// </summary>
/// <param name="Id">The cycle.</param>
/// <param name="CycleName">What it is called.</param>
/// <param name="StartDate">When counting began.</param>
public sealed record OpenVerificationCycleResponse(
    int Id,
    string CycleName,
    DateOnly StartDate);
