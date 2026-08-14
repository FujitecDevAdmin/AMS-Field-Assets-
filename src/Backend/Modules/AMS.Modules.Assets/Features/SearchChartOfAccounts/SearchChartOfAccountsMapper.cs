namespace AMS.Modules.Assets.Features.SearchChartOfAccounts;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchChartOfAccountsMapper
{
    public static SearchChartOfAccountsQuery ToQuery(SearchChartOfAccountsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchChartOfAccountsQuery(
            request.IsActive);
    }
}
