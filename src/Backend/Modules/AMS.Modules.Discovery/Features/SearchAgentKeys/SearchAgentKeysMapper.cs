namespace AMS.Modules.Discovery.Features.SearchAgentKeys;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchAgentKeysMapper
{
    public static SearchAgentKeysQuery ToQuery(SearchAgentKeysRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchAgentKeysQuery(
            request.ActiveOnly ?? false);
    }
}
