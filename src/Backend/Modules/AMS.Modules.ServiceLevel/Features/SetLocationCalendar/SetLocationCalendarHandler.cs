using AMS.Modules.Organization.PublicApi.Organization;
using AMS.Modules.ServiceLevel.Calendar;
using AMS.Modules.ServiceLevel.Domain;
using AMS.Modules.ServiceLevel.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Features.SetLocationCalendar;

/// <summary>
/// Set a branch's working week. Catalogue: Operational Hours Setup.
/// </summary>
/// <remarks>
/// <para>
/// The whole calendar arrives at once — the standard window, all seven
/// weekdays and the five Saturday occurrence rules. A calendar is one thing,
/// and an endpoint per part would let a half-saved one exist; the SLA service
/// would read it and believe it, and every due date computed in between would
/// be wrong in a way nobody could see afterwards.
/// </para>
/// <para>
/// It is also an upsert. A branch has one calendar or none
/// (UX_LocationOperationalHour_Location), so "create" and "edit" are the same
/// act from the screen's point of view and pretending otherwise would make the
/// client ask which one it is doing.
/// </para>
/// </remarks>
public sealed class SetLocationCalendarHandler(
    ServiceLevelDbContext db,
    IBranchDirectory locations,
    CalendarLoader calendars,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<SetLocationCalendarCommand, SetLocationCalendarResponse>
{
    public async Task<Result<SetLocationCalendarResponse>> HandleAsync(
        SetLocationCalendarCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await locations.IsActiveAsync(request.LocationId, ct))
        {
            return Error.NotFound("Location", request.LocationId);
        }

        var invalid = Validate(request);
        if (invalid is not null)
        {
            return invalid;
        }

        var now = clock.UtcNow;

        var hours = await db.LocationOperationalHours
            .SingleOrDefaultAsync(h => h.LocationId == request.LocationId, ct);

        if (hours is null)
        {
            hours = new LocationOperationalHour
            {
                LocationId = request.LocationId,
                CreatedOnUtc = now,
                CreatedBy = currentUser.Username,
                StandardStartTime = request.StandardStartTime,
                StandardEndTime = request.StandardEndTime,
            };

            db.LocationOperationalHours.Add(hours);
        }

        hours.IsRoundTheClock = request.IsRoundTheClock;
        hours.StandardStartTime = request.StandardStartTime;
        hours.StandardEndTime = request.StandardEndTime;
        hours.BreakStartTime = request.BreakStartTime;
        hours.BreakEndTime = request.BreakEndTime;
        hours.DeferFinalMinutes = request.DeferFinalMinutes;
        hours.DeferNewTicketsOnFriday = request.DeferNewTicketsOnFriday;
        hours.IsActive = true;
        hours.ModifiedOnUtc = now;
        hours.ModifiedBy = currentUser.Username;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        await ReplaceDaysAsync(hours.Id, request, ct);
        await ReplaceSaturdaysAsync(hours.Id, request.WorkingSaturdays, ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        // The loader caches per request, and this request has just invalidated
        // its own cache. Anything reading the calendar after this point in the
        // same request would otherwise get the week as it was before the edit.
        calendars.Forget(request.LocationId);

        return new SetLocationCalendarResponse(
            request.LocationId,
            request.Days.Count(d => d.IsWorkingDay),
            request.WorkingSaturdays.Count);
    }

    /// <summary>
    /// Everything the CHECK constraints would reject, and two things they
    /// cannot see.
    /// </summary>
    private static Error? Validate(SetLocationCalendarCommand request)
    {
        if (!request.IsRoundTheClock && request.StandardEndTime <= request.StandardStartTime)
        {
            return Error.Validation(
                "LocationCalendar.Window",
                "The branch must close after it opens.");
        }

        if (request.BreakStartTime is null != (request.BreakEndTime is null))
        {
            return Error.Validation(
                "LocationCalendar.BreakPair",
                "A break needs both a start and an end.");
        }

        if (request.BreakStartTime is { } breakStart && request.BreakEndTime is { } breakEnd)
        {
            if (breakEnd <= breakStart)
            {
                return Error.Validation(
                    "LocationCalendar.BreakOrder",
                    "The break must end after it starts.");
            }

            // A break outside the working window silently removes nothing,
            // which looks exactly like the configuration having worked.
            if (!request.IsRoundTheClock
                && (breakStart < request.StandardStartTime || breakEnd > request.StandardEndTime))
            {
                return Error.Validation(
                    "LocationCalendar.BreakOutsideWindow",
                    "The break has to fall inside the working window.");
            }
        }

        var seen = new HashSet<byte>();

        foreach (var day in request.Days)
        {
            if (day.DayOfWeek > 6)
            {
                return Error.Validation(
                    "LocationCalendar.DayOfWeek",
                    "A weekday is 0 (Sunday) through 6 (Saturday).");
            }

            if (!seen.Add(day.DayOfWeek))
            {
                return Error.Validation(
                    "LocationCalendar.DuplicateDay",
                    "Each weekday may appear once.");
            }

            if (!CalendarDayType.Allowed.Contains(day.DayType, StringComparer.Ordinal))
            {
                return Error.Validation(
                    "LocationCalendar.DayType",
                    $"Day type must be one of {string.Join(", ", CalendarDayType.Allowed)}.");
            }

            if (day.DayType == CalendarDayType.Custom
                && (day.StartTime is null || day.EndTime is null || day.EndTime <= day.StartTime))
            {
                return Error.Validation(
                    "LocationCalendar.CustomTimes",
                    "A Custom day needs a start and an end, and must close after it opens.");
            }
        }

        // Seven or nothing. Six rows would leave the seventh to a fallback, and
        // a branch whose Wednesday came from a default nobody chose is the kind
        // of thing that is only noticed when a Wednesday ticket is late.
        if (request.Days.Count is not 0 and not 7)
        {
            return Error.Validation(
                "LocationCalendar.SevenDays",
                "Send all seven weekdays, or none to keep the default week.");
        }

        return request.WorkingSaturdays.Any(o => o is < 1 or > 5)
            ? Error.Validation(
                "LocationCalendar.SaturdayOccurrence",
                "A Saturday occurrence is 1 through 5.")
            : null;
    }

    /// <summary>
    /// Replaces the seven weekday rows.
    /// </summary>
    /// <remarks>
    /// Delete and re-insert rather than a diff. There are seven of them, they
    /// carry no identity anybody refers to, and a diff would be more code doing
    /// the same thing less obviously.
    /// </remarks>
    private async Task ReplaceDaysAsync(
        int hoursId,
        SetLocationCalendarCommand request,
        CancellationToken ct)
    {
        if (request.Days.Count == 0)
        {
            return;
        }

        var existing = await db.LocationOperationalDays
            .Where(d => d.LocationOperationalHourId == hoursId)
            .ToListAsync(ct);

        db.LocationOperationalDays.RemoveRange(existing);
        await db.SaveChangesAsync(ct);

        foreach (var day in request.Days)
        {
            db.LocationOperationalDays.Add(new LocationOperationalDay
            {
                LocationOperationalHourId = hoursId,
                DayOfWeek = day.DayOfWeek,
                IsWorkingDay = day.IsWorkingDay,
                DayType = day.DayType,
                // A Standard day stores no times: it means "whatever the
                // standard window is now", and seven copies of it would go
                // stale the first time somebody edits that window.
                StartTime = day.DayType == CalendarDayType.Custom ? day.StartTime : null,
                EndTime = day.DayType == CalendarDayType.Custom ? day.EndTime : null,
                BreakStartTime = day.DayType == CalendarDayType.Custom ? day.BreakStartTime : null,
                BreakEndTime = day.DayType == CalendarDayType.Custom ? day.BreakEndTime : null,
            });
        }
    }

    private async Task ReplaceSaturdaysAsync(
        int hoursId,
        IReadOnlyList<int> working,
        CancellationToken ct)
    {
        var existing = await db.LocationSaturdayRules
            .Where(s => s.LocationOperationalHourId == hoursId)
            .ToListAsync(ct);

        db.LocationSaturdayRules.RemoveRange(existing);
        await db.SaveChangesAsync(ct);

        if (working.Count == 0)
        {
            // No rules at all means every Saturday follows the weekday row.
            // Writing five "not working" rows instead would silently close a
            // branch that had simply not answered the question.
            return;
        }

        foreach (var occurrence in Enumerable.Range(1, 5))
        {
            db.LocationSaturdayRules.Add(new LocationSaturdayRule
            {
                LocationOperationalHourId = hoursId,
                Occurrence = (byte)occurrence,
                IsWorking = working.Contains(occurrence),
            });
        }
    }
}
