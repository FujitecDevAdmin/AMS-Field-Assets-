using AMS.Modules.ServiceLevel.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Features.SearchHolidays;

/// <summary>
/// The holiday calendar. Catalogue: Holiday Calendar.
/// </summary>
/// <remarks>
/// Filtering by branch returns the holidays that branch actually observes: the
/// ones attached to it AND the ones that apply everywhere. A screen that showed
/// only the attached ones would tell a branch manager their branch works on
/// Republic Day.
/// </remarks>
public sealed class SearchHolidaysHandler(ServiceLevelDbContext db)
    : IRequestHandler<SearchHolidaysQuery, SearchHolidaysResponse>
{
    public async Task<Result<SearchHolidaysResponse>> HandleAsync(
        SearchHolidaysQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.HolidayCalendars.AsNoTracking();

        if (request.ActiveOnly)
        {
            query = query.Where(h => h.IsActive);
        }

        if (request.Year is { } year)
        {
            // A recurring holiday belongs to every year, not to the one it
            // happened to be entered in.
            query = query.Where(h => h.HolidayYear == year || h.IsRecurringAnnually);
        }

        if (request.HolidayType is { } type)
        {
            query = query.Where(h => h.HolidayType == type);
        }

        if (request.LocationId is { } locationId)
        {
            query = query.Where(h => h.AppliesToAllLocations
                || db.HolidayLocations.Any(
                    l => l.HolidayCalendarId == h.Id && l.LocationId == locationId));
        }

        var holidays = await query
            .OrderBy(h => h.HolidayDate)
            .ThenBy(h => h.HolidayName)
            .ToListAsync(ct);

        var ids = holidays.ConvertAll(h => h.Id);

        var attachments = await db.HolidayLocations
            .AsNoTracking()
            .Where(l => ids.Contains(l.HolidayCalendarId))
            .Select(l => new { l.HolidayCalendarId, l.LocationId })
            .ToListAsync(ct);

        var byHoliday = attachments
            .GroupBy(l => l.HolidayCalendarId)
            .ToDictionary(g => g.Key, g => g.Select(l => l.LocationId).Order().ToList());

        var rows = holidays.ConvertAll(h => new SearchHolidaysResponse.Row(
            h.Id,
            h.HolidayName,
            h.HolidayDate,
            h.HolidayYear,
            h.HolidayType,
            h.AppliesToAllLocations,
            h.IsRecurringAnnually,
            h.RecurrenceMonth,
            h.RecurrenceDay,
            h.Remarks,
            h.IsActive,
            byHoliday.TryGetValue(h.Id, out var own) ? own : []));

        return new SearchHolidaysResponse(rows);
    }
}
