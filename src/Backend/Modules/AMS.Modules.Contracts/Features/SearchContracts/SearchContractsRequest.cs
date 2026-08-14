namespace AMS.Modules.Contracts.Features.SearchContracts;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchContractsRequest(
    string? Search,
    string? ContractType,
    int? VendorId,
    int? ExpiringWithinDays,
    bool? IncludeExpired,
    int? Skip,
    int? Take);
