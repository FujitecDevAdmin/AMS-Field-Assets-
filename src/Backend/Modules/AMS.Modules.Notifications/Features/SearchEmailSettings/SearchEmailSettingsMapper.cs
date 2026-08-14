namespace AMS.Modules.Notifications.Features.SearchEmailSettings;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchEmailSettingsMapper
{
    public static SearchEmailSettingsQuery ToQuery(SearchEmailSettingsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchEmailSettingsQuery(
            request.ActiveOnly ?? false);
    }
}
