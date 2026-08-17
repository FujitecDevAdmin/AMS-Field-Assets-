namespace AMS.Modules.Organization.Features.SearchEmployees;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchEmployeesMapper
{
    public static SearchEmployeesQuery ToQuery(SearchEmployeesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchEmployeesQuery(
            request.Search?.Trim(),
            request.DepartmentId,
            request.BranchId,
            request.IsActive,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
