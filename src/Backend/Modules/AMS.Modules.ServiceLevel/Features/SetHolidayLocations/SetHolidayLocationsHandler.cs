using AMS.Modules.ServiceLevel.Domain;
using AMS.Modules.ServiceLevel.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Features.SetHolidayLocations;

/// <summary>
/// Say which branches observe a regional holiday. Catalogue: Holiday Calendar.
/// </summary>
/// <remarks>
/// The whole set at once, for the reason a support team's membership is: add
/// and remove endpoints would make the screen compute a difference against a
/// list that may have moved under it.
/// </remarks>
public sealed class SetHolidayLocationsHandler(
    ServiceLevelDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<SetHolidayLocationsCommand, SetHolidayLocationsResponse>
{
    public async Task<Result<SetHolidayLocationsResponse>> HandleAsync(
        SetHolidayLocationsCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var holiday = await db.HolidayCalendars.SingleOrDefaultAsync(h => h.Id == request.Id, ct);
        if (holiday is null)
        {
            return Error.NotFound("Holiday", request.Id);
        }

        var wanted = request.LocationIds.Distinct().ToList();

        if (!holiday.AppliesToAllLocations && wanted.Count == 0)
        {
            return Error.Validation(
                "Holiday.NoLocations",
                "A holiday that does not apply to all branches must name at least one.");
        }

        var existing = await db.HolidayLocations
            .Where(l => l.HolidayCalendarId == holiday.Id)
            .ToListAsync(ct);

        // Kept rather than replaced wholesale, so CreatedOnUtc still says when
        // a branch actually started observing it.
        db.HolidayLocations.RemoveRange(
            existing.Where(l => !wanted.Contains(l.LocationId)));

        var now = clock.UtcNow;

        foreach (var locationId in wanted.Where(id => existing.TrueForAll(l => l.LocationId != id)))
        {
            db.HolidayLocations.Add(new HolidayLocation
            {
                HolidayCalendarId = holiday.Id,
                LocationId = locationId,
                CreatedOnUtc = now,
                CreatedBy = currentUser.Username,
            });
        }

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

        return new SetHolidayLocationsResponse(holiday.Id, wanted.Count);
    }
}
