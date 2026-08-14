using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Verification.Features.SearchVerifications;

/// <summary>
/// What was found, and what was not. Catalogue: the exception report.
/// </summary>
public sealed record SearchVerificationsQuery(
    int? CycleId,
    int? LocationId,
    string? WorkingCondition,
    bool ExceptionsOnly,
    bool MismatchesOnly,
    int Skip,
    int Take) : IQuery<SearchVerificationsResponse>;
