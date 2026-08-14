namespace AMS.Modules.Allocations.Features.SearchHandovers;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchHandoversMapper
{
    public static SearchHandoversQuery ToQuery(SearchHandoversRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchHandoversQuery(
            string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
            request.BranchLocationId,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
