namespace AMS.Modules.Allocations.Features.SearchAllocationRequests;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchAllocationRequestsMapper
{
    public static SearchAllocationRequestsQuery ToQuery(SearchAllocationRequestsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchAllocationRequestsQuery(
            string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
            request.EmployeeId,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
