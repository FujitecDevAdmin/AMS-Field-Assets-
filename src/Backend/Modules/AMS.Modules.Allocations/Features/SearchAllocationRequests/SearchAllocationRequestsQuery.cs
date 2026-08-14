using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.SearchAllocationRequests;

/// <summary>
/// The approval queue. Catalogue screen: Allocation Requests.
/// </summary>
public sealed record SearchAllocationRequestsQuery(
    string? Status,
    int? EmployeeId,
    int Skip,
    int Take) : IQuery<SearchAllocationRequestsResponse>;
