namespace AMS.Modules.ServiceDesk.Features.SearchRequestCategories;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchRequestCategoriesMapper
{
    public static SearchRequestCategoriesQuery ToQuery(SearchRequestCategoriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchRequestCategoriesQuery(
            request.IsActive);
    }
}
