namespace AMS.Modules.ServiceLevel.Features.SearchHolidays;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchHolidaysMapper
{
    public static SearchHolidaysQuery ToQuery(SearchHolidaysRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchHolidaysQuery(
            request.Year,
            string.IsNullOrWhiteSpace(request.HolidayType) ? null : request.HolidayType.Trim(),
            request.LocationId,
            request.ActiveOnly ?? true);
    }
}
