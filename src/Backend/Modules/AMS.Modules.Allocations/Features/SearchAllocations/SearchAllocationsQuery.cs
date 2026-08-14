using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.SearchAllocations;

/// <summary>
/// Live allocations, expected returns and the overdue list. Catalogue screen: Allocations.
/// </summary>
public sealed record SearchAllocationsQuery(
    int? AssetId,
    int? EmployeeId,
    int? LocationId,
    bool OpenOnly,
    bool OverdueOnly,
    int Skip,
    int Take) : IQuery<SearchAllocationsResponse>;
