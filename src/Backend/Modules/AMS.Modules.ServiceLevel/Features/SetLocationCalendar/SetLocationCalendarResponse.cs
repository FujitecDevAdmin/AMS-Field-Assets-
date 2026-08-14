namespace AMS.Modules.ServiceLevel.Features.SetLocationCalendar;

/// <summary>
/// The week as it now stands.
/// </summary>
/// <param name="LocationId">The branch.</param>
/// <param name="WorkingDayCount">How many weekdays are worked.</param>
/// <param name="WorkingSaturdayCount">How many Saturdays of the month, when Saturday is worked at all.</param>
public sealed record SetLocationCalendarResponse(
    int LocationId,
    int WorkingDayCount,
    int WorkingSaturdayCount);
