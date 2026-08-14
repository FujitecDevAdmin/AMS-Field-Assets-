namespace AMS.Modules.ServiceLevel.Features.UpdateHoliday;

/// <summary>
/// The holiday as it now stands.
/// </summary>
/// <param name="Id">The holiday.</param>
/// <param name="HolidayName">What it is called.</param>
/// <param name="IsActive">Whether the calendar still observes it.</param>
public sealed record UpdateHolidayResponse(
    int Id,
    string HolidayName,
    bool IsActive);
