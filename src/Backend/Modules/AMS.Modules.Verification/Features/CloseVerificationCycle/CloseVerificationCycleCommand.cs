using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Verification.Features.CloseVerificationCycle;

/// <summary>
/// Finish a verification round. Catalogue: Verification Cycles.
/// </summary>
public sealed record CloseVerificationCycleCommand(
    int Id) : ICommand<CloseVerificationCycleResponse>;
