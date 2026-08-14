using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Verification.Features.OpenVerificationCycle;

/// <summary>
/// Start a verification round. Catalogue: Verification Cycles.
/// </summary>
public sealed record OpenVerificationCycleCommand(
    string CycleName,
    DateOnly StartDate,
    DateOnly? EndDate) : ICommand<OpenVerificationCycleResponse>;
