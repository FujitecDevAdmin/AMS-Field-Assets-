namespace AMS.Modules.Notifications.Features.SearchEmailOutbox;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchEmailOutboxMapper
{
    public static SearchEmailOutboxQuery ToQuery(SearchEmailOutboxRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchEmailOutboxQuery(
            string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
            string.IsNullOrWhiteSpace(request.SourceType) ? null : request.SourceType.Trim(),
            request.SourceId,
            string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
