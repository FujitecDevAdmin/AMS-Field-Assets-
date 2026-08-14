using AMS.Modules.ServiceLevel.Domain;
using AMS.Modules.ServiceLevel.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Features.CreateHoliday;

/// <summary>
/// Add a holiday. Catalogue: Holiday Calendar.
/// </summary>
/// <remarks>
/// The branches it applies to arrive with it. A holiday saved with no branches
/// and attached a minute later is a holiday that was briefly observed nowhere,
/// and if the second call never happens it stays that way looking correct.
/// </remarks>
public sealed class CreateHolidayHandler(
    ServiceLevelDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<CreateHolidayCommand, CreateHolidayResponse>
{
    public async Task<Result<CreateHolidayResponse>> HandleAsync(
        CreateHolidayCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var invalid = HolidayRules.Validate(
            request.HolidayType, request.HolidayDate,
            request.AppliesToAllLocations, request.LocationIds);

        if (invalid is not null)
        {
            return invalid;
        }

        var now = clock.UtcNow;

        var holiday = new HolidayCalendar
        {
            HolidayName = request.HolidayName,
            HolidayDate = request.HolidayDate,
            // CK_HolidayCalendar_YearMatchesDate requires these to agree, so
            // the year is derived rather than accepted. A client that could
            // send both could send two that disagree.
            HolidayYear = request.HolidayDate.Year,
            HolidayType = request.HolidayType,
            AppliesToAllLocations = request.AppliesToAllLocations,
            IsRecurringAnnually = request.IsRecurringAnnually,
            RecurrenceMonth = request.IsRecurringAnnually ? (byte)request.HolidayDate.Month : null,
            RecurrenceDay = request.IsRecurringAnnually ? (byte)request.HolidayDate.Day : null,
            Remarks = request.Remarks,
            IsActive = true,
            CreatedOnUtc = now,
            CreatedBy = currentUser.Username,
        };

        db.HolidayCalendars.Add(holiday);

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

        if (!request.AppliesToAllLocations)
        {
            foreach (var locationId in request.LocationIds.Distinct())
            {
                db.HolidayLocations.Add(new HolidayLocation
                {
                    HolidayCalendarId = holiday.Id,
                    LocationId = locationId,
                    CreatedOnUtc = now,
                    CreatedBy = currentUser.Username,
                });
            }

            await db.SaveChangesAsync(ct);
        }

        return new CreateHolidayResponse(
            holiday.Id,
            holiday.HolidayName,
            holiday.HolidayDate,
            request.AppliesToAllLocations ? 0 : request.LocationIds.Distinct().Count());
    }
}

/// <summary>Rules the create and update slices share.</summary>
public static class HolidayRules
{
    /// <summary>Everything the CHECK constraints would reject, refused by name.</summary>
    public static Error? Validate(
        string holidayType,
        DateOnly date,
        bool appliesToAll,
        IReadOnlyList<int> locationIds)
    {
        ArgumentNullException.ThrowIfNull(locationIds);

        if (!HolidayType.Allowed.Contains(holidayType, StringComparer.Ordinal))
        {
            return Error.Validation(
                "Holiday.UnknownType",
                $"Holiday type must be one of {string.Join(", ", HolidayType.Allowed)}.");
        }

        if (date.Year is < 2000 or > 2100)
        {
            return Error.Validation(
                "Holiday.Year",
                "A holiday falls between 2000 and 2100.");
        }

        // A regional holiday attached to nothing is observed nowhere, which
        // looks exactly like it working. The stored AppliesToAllLocations flag
        // exists so the two mistakes cannot be confused; this stops the second
        // one being made silently.
        return !appliesToAll && locationIds.Count == 0
            ? Error.Validation(
                "Holiday.NoLocations",
                "Name the branches that observe it, or mark it as applying to all of them.")
            : null;
    }
}
