namespace AMS.Modules.Organization.Features.SearchBranches;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchBranchesMapper
{
    public static SearchBranchesQuery ToQuery(SearchBranchesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchBranchesQuery(
            request.IsActive,
            request.RegionId,
            request.Search?.Trim());
    }
}
