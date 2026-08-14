using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Contracts.Features.SearchContracts;

/// <summary>
/// Contracts, and the ones about to run out. Catalogue: Contracts.
/// </summary>
public sealed record SearchContractsQuery(
    string? Search,
    string? ContractType,
    int? VendorId,
    int? ExpiringWithinDays,
    bool IncludeExpired,
    int Skip,
    int Take) : IQuery<SearchContractsResponse>;
