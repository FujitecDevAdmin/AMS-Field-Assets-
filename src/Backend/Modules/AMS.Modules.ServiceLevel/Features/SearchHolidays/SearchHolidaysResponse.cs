namespace AMS.Modules.ServiceLevel.Features.SearchHolidays;

/// <summary>
/// Holidays, earliest first.
/// </summary>
/// <param name="Rows">The list, each with the branches that observe it.</param>
public sealed record SearchHolidaysResponse(
    IReadOnlyList<SearchHolidaysResponse.Row> Rows)
{
    /// <summary>One holiday.</summary>
    /// <param name="Id">The holiday.</param>
    /// <param name="HolidayName">What it is called.</param>
    /// <param name="HolidayDate">The date it falls on, for the year it was entered.</param>
    /// <param name="HolidayYear">That year. CK_HolidayCalendar_YearMatchesDate keeps the two together.</param>
    /// <param name="HolidayType">Government, Festival, Regional or Optional.</param>
    /// <param name="AppliesToAllLocations">
    /// Stored, not inferred from an empty location list: a holiday for
    /// everybody and a regional one somebody forgot to attach branches to are
    /// different mistakes and must not look identical.
    /// </param>
    /// <param name="IsRecurringAnnually">Whether it repeats without re-entering.</param>
    /// <param name="RecurrenceMonth">The month it repeats in.</param>
    /// <param name="RecurrenceDay">The day of that month.</param>
    /// <param name="Remarks">Anything else.</param>
    /// <param name="IsActive">Whether the calendar observes it.</param>
    /// <param name="LocationIds">The branches that observe it, when it is not for all of them.</param>
    public sealed record Row(
        int Id,
        string HolidayName,
        DateOnly HolidayDate,
        int HolidayYear,
        string HolidayType,
        bool AppliesToAllLocations,
        bool IsRecurringAnnually,
        byte? RecurrenceMonth,
        byte? RecurrenceDay,
        string? Remarks,
        bool IsActive,
        IReadOnlyList<int> LocationIds);
}
