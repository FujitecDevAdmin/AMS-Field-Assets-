using AMS.Modules.Organization.Domain;
using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.CreateRegion;

/// <summary>
/// Add a region. Catalogue: Regions.
/// </summary>
/// <remarks>
/// Regions exist so tickets route by a master-data row instead of by matching
/// branch names against a hard-coded list — which is how a new branch silently
/// landed in the wrong queue on the day it opened.
/// </remarks>
public sealed class CreateRegionHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CreateRegionCommand, CreateRegionResponse>
{
    public async Task<Result<CreateRegionResponse>> HandleAsync(CreateRegionCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var region = new Region
        {
            RegionName = request.RegionName,
            Description = request.Description,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.Regions.Add(region);

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

        return new CreateRegionResponse(region.Id, region.RegionName);
    }
}
