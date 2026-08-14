namespace AMS.Modules.ServiceLevel.Features.SearchHolidays;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchHolidaysRequest(
    int? Year,
    string? HolidayType,
    int? LocationId,
    bool? ActiveOnly);
