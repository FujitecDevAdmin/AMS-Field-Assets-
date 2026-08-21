using AMS.Modules.Organization.PublicApi.Organization;
using AMS.Modules.ServiceLevel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Calendar;

/// <summary>
/// Assembles a branch's <see cref="CalendarSnapshot"/> from its rows.
/// </summary>
/// <remarks>
/// <para>
/// Four reads, cached for the life of the request. The SLA service asks the
/// same question about the same branch many times while computing one
/// answer — every span it measures needs the calendar again — and four queries
/// per span would make the arithmetic the cheap part of the arithmetic.
/// </para>
/// <para>
/// Scoped, so the cache lasts one request and no longer. A calendar edited on
/// the setup screen takes effect on the next request rather than whenever a
/// process happens to restart, which is the behaviour anybody editing it
/// expects.
/// </para>
/// </remarks>
public sealed class CalendarLoader(ServiceLevelDbContext db, IBranchDirectory locations)
{
    private readonly Dictionary<int, CalendarSnapshot> _cache = [];

    /// <summary>
    /// The branch's calendar. A branch with no configuration gets the default
    /// one rather than an error: it still raises tickets.
    /// </summary>
    public async Task<CalendarSnapshot> LoadAsync(int locationId, CancellationToken ct)
    {
        if (_cache.TryGetValue(locationId, out var cached))
        {
            return cached;
        }

        var timeZone = Resolve(await locations.TimeZoneOfAsync(locationId, ct));

        var hours = await db.LocationOperationalHours
            .AsNoTracking()
            .SingleOrDefaultAsync(h => h.LocationId == locationId && h.IsActive, ct);

        var holidays = await LoadHolidaysAsync(locationId, ct);

        CalendarSnapshot snapshot;

        if (hours is null)
        {
            var fallback = CalendarSnapshot.Default(locationId, timeZone);

            // Even an unconfigured branch observes the holidays somebody has
            // entered for it. The default is about HOURS, not about pretending
            // Republic Day is a working day.
            snapshot = fallback with
            {
                FixedHolidays = holidays.Fixed,
                RecurringHolidays = holidays.Recurring,
            };
        }
        else
        {
            var days = await db.LocationOperationalDays
                .AsNoTracking()
                .Where(d => d.LocationOperationalHourId == hours.Id)
                .ToListAsync(ct);

            var saturdays = await db.LocationSaturdayRules
                .AsNoTracking()
                .Where(s => s.LocationOperationalHourId == hours.Id && s.IsWorking)
                .Select(s => (int)s.Occurrence)
                .ToListAsync(ct);

            snapshot = new CalendarSnapshot(
                locationId,
                timeZone,
                hours.IsRoundTheClock,
                hours.StandardStartTime,
                hours.StandardEndTime,
                hours.BreakStartTime,
                hours.BreakEndTime,
                hours.DeferFinalMinutes,
                hours.DeferNewTicketsOnFriday,
                [.. Enum.GetValues<DayOfWeek>().Select(day => Day(days, day))],
                saturdays.ToHashSet(),
                holidays.Fixed,
                holidays.Recurring);
        }

        _cache[locationId] = snapshot;

        return snapshot;
    }

    /// <summary>
    /// Whether anybody has actually set this branch up.
    /// </summary>
    /// <remarks>
    /// <see cref="LoadAsync"/> answers with the default week when nobody has,
    /// which is right for the arithmetic and wrong for the setup screen: an
    /// administrator needs to know whether they are looking at a decision or at
    /// a fallback.
    /// </remarks>
    public async Task<bool> IsConfiguredAsync(int locationId, CancellationToken ct) =>
        await db.LocationOperationalHours
            .AsNoTracking()
            .AnyAsync(h => h.LocationId == locationId && h.IsActive, ct);

    /// <summary>Forgets a branch, so the next read sees an edit.</summary>
    public void Forget(int locationId) => _cache.Remove(locationId);

    /// <summary>
    /// A weekday row, or a sensible one when the seven have not all been
    /// written.
    /// </summary>
    /// <remarks>
    /// The setup slice writes all seven together, so a missing row means
    /// somebody has been in the database by hand. Monday to Friday is a better
    /// answer than a branch that silently never opens on Wednesdays.
    /// </remarks>
    private static CalendarDay Day(
        IReadOnlyList<Domain.LocationOperationalDay> rows,
        DayOfWeek day)
    {
        var row = rows.FirstOrDefault(d => d.DayOfWeek == (byte)day);

        return row is null
            ? new CalendarDay(
                day,
                day is not (DayOfWeek.Saturday or DayOfWeek.Sunday),
                CalendarDayType.Standard,
                null, null, null, null)
            : new CalendarDay(
                day,
                row.IsWorkingDay,
                row.DayType,
                row.StartTime,
                row.EndTime,
                row.BreakStartTime,
                row.BreakEndTime);
    }

    /// <summary>
    /// The holidays this branch observes: the ones for everybody, plus the ones
    /// attached to it.
    /// </summary>
    /// <remarks>
    /// <c>AppliesToAllLocations</c> is stored rather than inferred from "no
    /// rows in HolidayLocation", because an all-location holiday and a regional
    /// holiday somebody forgot to attach locations to are different mistakes
    /// and must not look identical. This reads the flag.
    /// </remarks>
    private async Task<(HashSet<DateOnly> Fixed, HashSet<(int, int)> Recurring)>
        LoadHolidaysAsync(int locationId, CancellationToken ct)
    {
        var rows = await db.HolidayCalendars
            .AsNoTracking()
            .Where(h => h.IsActive)
            .Where(h => h.AppliesToAllLocations
                || db.HolidayLocations.Any(
                    l => l.HolidayCalendarId == h.Id && l.LocationId == locationId))
            .Select(h => new
            {
                h.HolidayDate,
                h.IsRecurringAnnually,
                h.RecurrenceMonth,
                h.RecurrenceDay,
            })
            .ToListAsync(ct);

        var fixedDates = new HashSet<DateOnly>();
        var recurring = new HashSet<(int, int)>();

        foreach (var row in rows)
        {
            if (row is { IsRecurringAnnually: true, RecurrenceMonth: { } month, RecurrenceDay: { } day })
            {
                recurring.Add((month, day));
            }
            else
            {
                fixedDates.Add(row.HolidayDate);
            }
        }

        return (fixedDates, recurring);
    }

    /// <summary>
    /// The branch's time zone, or India Standard Time.
    /// </summary>
    /// <remarks>
    /// A zone id the machine does not recognise must not stop a ticket being
    /// raised. Falling back is wrong by some hours; throwing is wrong by the
    /// whole feature, and the id is a column an administrator can correct.
    /// </remarks>
    private static TimeZoneInfo Resolve(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            timeZoneId = "India Standard Time";
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        }
    }
}
