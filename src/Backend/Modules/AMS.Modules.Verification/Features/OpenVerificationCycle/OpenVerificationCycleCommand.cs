using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Verification.Features.OpenVerificationCycle;

/// <summary>
/// Start a verification round. Catalogue: Verification Cycles.
/// </summary>
public sealed record OpenVerificationCycleCommand(
    string CycleName,
    int BranchId,
    DateOnly StartDate,
    DateOnly? EndDate,
    IReadOnlyList<int> AuditorUserIds,
    IReadOnlyList<int> LocationBranchIds) : ICommand<OpenVerificationCycleResponse>;
