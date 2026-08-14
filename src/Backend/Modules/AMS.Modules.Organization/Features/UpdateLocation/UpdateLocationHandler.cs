using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.UpdateLocation;

/// <summary>
/// Edit a branch, move it between regions, or retire it.
/// </summary>
/// <remarks>
/// Moving the head-office flag from one branch to another in two separate
/// requests transiently leaves two branches flagged, and
/// <c>UX_Location_OneHeadOffice</c> rejects the second one with a 409. That is
/// the correct outcome: the administrator must clear the old head office
/// first, and the message says so.
/// </remarks>
public sealed class UpdateLocationHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateLocationCommand, UpdateLocationResponse>
{
    public async Task<Result<UpdateLocationResponse>> HandleAsync(
        UpdateLocationCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var location = await db.Locations.SingleOrDefaultAsync(l => l.Id == request.Id, ct);
        if (location is null)
        {
            return Error.NotFound("Location", request.Id);
        }

        location.LocationCode = request.LocationCode;
        location.LocationName = request.LocationName;
        location.RegionId = request.RegionId;
        location.TimeZoneId = request.TimeZoneId;
        location.IsHeadOffice = request.IsHeadOffice;
        location.IsActive = request.IsActive;
        location.ModifiedOnUtc = clock.UtcNow;
        location.ModifiedBy = currentUser.Username;

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

        return new UpdateLocationResponse(
            location.Id, location.LocationCode, location.IsHeadOffice, location.IsActive);
    }
}
