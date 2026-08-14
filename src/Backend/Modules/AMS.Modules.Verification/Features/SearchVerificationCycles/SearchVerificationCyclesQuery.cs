using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Verification.Features.SearchVerificationCycles;

/// <summary>
/// The verification cycles. Catalogue: Verification Cycles.
/// </summary>
public sealed record SearchVerificationCyclesQuery(
    bool ActiveOnly) : IQuery<SearchVerificationCyclesResponse>;
