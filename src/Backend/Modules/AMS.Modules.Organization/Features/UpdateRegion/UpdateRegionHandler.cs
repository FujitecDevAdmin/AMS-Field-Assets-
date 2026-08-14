using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.UpdateRegion;

/// <summary>Rename a region or retire it. Catalogue screen: Regions.</summary>
public sealed class UpdateRegionHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateRegionCommand, UpdateRegionResponse>
{
    public async Task<Result<UpdateRegionResponse>> HandleAsync(UpdateRegionCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var region = await db.Regions.SingleOrDefaultAsync(r => r.Id == request.Id, ct);
        if (region is null)
        {
            return Error.NotFound("Region", request.Id);
        }

        region.RegionName = request.RegionName;
        region.Description = request.Description;
        region.IsActive = request.IsActive;
        region.ModifiedOnUtc = clock.UtcNow;
        region.ModifiedBy = currentUser.Username;

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

        return new UpdateRegionResponse(region.Id, region.RegionName, region.IsActive);
    }
}
