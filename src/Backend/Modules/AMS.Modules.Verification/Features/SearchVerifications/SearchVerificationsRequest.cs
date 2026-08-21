namespace AMS.Modules.Verification.Features.SearchVerifications;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchVerificationsRequest(
    int? CycleId,
    int? LocationId,
    string? WorkingCondition,
    bool? ExceptionsOnly,
    bool? MismatchesOnly,
    int? Skip,
    int? Take,
    int? BranchId = null,
    string? Location = null,
    string? Search = null);
