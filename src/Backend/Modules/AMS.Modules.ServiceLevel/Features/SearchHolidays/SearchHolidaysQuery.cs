using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceLevel.Features.SearchHolidays;

/// <summary>
/// The holiday calendar. Catalogue: Holiday Calendar.
/// </summary>
public sealed record SearchHolidaysQuery(
    int? Year,
    string? HolidayType,
    int? LocationId,
    bool ActiveOnly) : IQuery<SearchHolidaysResponse>;
