using AMS.Modules.ServiceLevel.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

using AMS.Modules.ServiceLevel.Features.CreateHoliday;

namespace AMS.Modules.ServiceLevel.Features.UpdateHoliday;

/// <summary>Edit a holiday or retire it. Catalogue: Holiday Calendar.</summary>
/// <remarks>
/// The branches are not edited here — that is
/// <c>SetHolidayLocations</c>. Changing the date and the branch list in one
/// call would make a partial failure ambiguous, and the two are separate acts
/// on the screen anyway.
/// </remarks>
public sealed class UpdateHolidayHandler(
    ServiceLevelDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<UpdateHolidayCommand, UpdateHolidayResponse>
{
    public async Task<Result<UpdateHolidayResponse>> HandleAsync(
        UpdateHolidayCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var holiday = await db.HolidayCalendars.SingleOrDefaultAsync(h => h.Id == request.Id, ct);
        if (holiday is null)
        {
            return Error.NotFound("Holiday", request.Id);
        }

        var attached = await db.HolidayLocations
            .Where(l => l.HolidayCalendarId == holiday.Id)
            .Select(l => l.LocationId)
            .ToListAsync(ct);

        var invalid = HolidayRules.Validate(
            request.HolidayType, request.HolidayDate, request.AppliesToAllLocations, attached);

        if (invalid is not null)
        {
            return invalid;
        }

        holiday.HolidayName = request.HolidayName;
        holiday.HolidayDate = request.HolidayDate;
        holiday.HolidayYear = request.HolidayDate.Year;
        holiday.HolidayType = request.HolidayType;
        holiday.AppliesToAllLocations = request.AppliesToAllLocations;
        holiday.IsRecurringAnnually = request.IsRecurringAnnually;
        holiday.RecurrenceMonth = request.IsRecurringAnnually ? (byte)request.HolidayDate.Month : null;
        holiday.RecurrenceDay = request.IsRecurringAnnually ? (byte)request.HolidayDate.Day : null;
        holiday.Remarks = request.Remarks;
        holiday.IsActive = request.IsActive;
        holiday.ModifiedOnUtc = clock.UtcNow;
        holiday.ModifiedBy = currentUser.Username;

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

        return new UpdateHolidayResponse(holiday.Id, holiday.HolidayName, holiday.IsActive);
    }
}
