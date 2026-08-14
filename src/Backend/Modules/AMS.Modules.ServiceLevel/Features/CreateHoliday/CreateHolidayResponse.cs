namespace AMS.Modules.ServiceLevel.Features.CreateHoliday;

/// <summary>
/// The holiday, as entered.
/// </summary>
/// <param name="Id">The holiday.</param>
/// <param name="HolidayName">What it is called.</param>
/// <param name="HolidayDate">The date it falls on this year.</param>
/// <param name="LocationCount">How many branches observe it. Zero when it applies to all of them.</param>
public sealed record CreateHolidayResponse(
    int Id,
    string HolidayName,
    DateOnly HolidayDate,
    int LocationCount);
