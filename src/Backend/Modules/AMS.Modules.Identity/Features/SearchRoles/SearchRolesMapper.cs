namespace AMS.Modules.Identity.Features.SearchRoles;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchRolesMapper
{
    public static SearchRolesQuery ToQuery(SearchRolesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchRolesQuery(
            request.IsActive);
    }
}
