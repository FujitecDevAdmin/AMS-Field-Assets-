namespace AMS.Modules.Allocations.Features.SearchAllocations;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchAllocationsMapper
{
    public static SearchAllocationsQuery ToQuery(SearchAllocationsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchAllocationsQuery(
            request.AssetId,
            request.EmployeeId,
            request.LocationId,
            request.OpenOnly ?? true,
            request.OverdueOnly ?? false,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
