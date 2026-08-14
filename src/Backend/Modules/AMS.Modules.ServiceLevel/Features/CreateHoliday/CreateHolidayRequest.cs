namespace AMS.Modules.ServiceLevel.Features.CreateHoliday;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateHolidayRequest(
    string HolidayName,
    DateOnly HolidayDate,
    string? HolidayType,
    bool? AppliesToAllLocations,
    bool? IsRecurringAnnually,
    string? Remarks,
    IReadOnlyList<int> LocationIds);
