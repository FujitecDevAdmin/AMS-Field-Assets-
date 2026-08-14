namespace AMS.Modules.ServiceLevel.Features.GetLocationCalendar;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetLocationCalendarMapper
{
    public static GetLocationCalendarQuery ToQuery(GetLocationCalendarRequest request, int locationId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetLocationCalendarQuery(
            locationId);
    }
}
