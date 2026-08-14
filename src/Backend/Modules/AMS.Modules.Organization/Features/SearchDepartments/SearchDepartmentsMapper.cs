namespace AMS.Modules.Organization.Features.SearchDepartments;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchDepartmentsMapper
{
    public static SearchDepartmentsQuery ToQuery(SearchDepartmentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchDepartmentsQuery(
            request.IsActive,
            request.Search?.Trim());
    }
}
