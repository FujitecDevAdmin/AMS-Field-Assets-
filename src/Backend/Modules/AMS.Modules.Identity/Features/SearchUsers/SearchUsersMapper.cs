namespace AMS.Modules.Identity.Features.SearchUsers;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchUsersMapper
{
    public static SearchUsersQuery ToQuery(SearchUsersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchUsersQuery(
            request.Search?.Trim(),
            request.IsActive,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
