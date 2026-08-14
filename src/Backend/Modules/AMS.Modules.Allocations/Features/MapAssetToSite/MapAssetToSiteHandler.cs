using AMS.Modules.Allocations.Domain;
using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.MapAssetToSite;

/// <summary>
/// Put an asset at a customer site. Catalogue: for equipment installed at a
/// customer rather than held by a person.
/// </summary>
/// <remarks>
/// UX_AssetSiteMapping_OneActivePerAsset - filtered on RemovedOnUtc IS NULL -
/// means an asset is at one site at a time. A second mapping collides on 2601
/// and returns a 409; no handler checks first.
/// </remarks>
public sealed class MapAssetToSiteHandler(
    AllocationsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<MapAssetToSiteCommand, MapAssetToSiteResponse>
{
    public async Task<Result<MapAssetToSiteResponse>> HandleAsync(
        MapAssetToSiteCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var site = await db.CustomerSites
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == request.CustomerSiteId, ct);
        if (site is null)
        {
            return Error.NotFound("CustomerSite", request.CustomerSiteId);
        }

        if (!site.IsActive)
        {
            return Error.Validation(
                "CustomerSite.Retired", "That site has been retired, so nothing new can be put on it.");
        }

        var mapping = new AssetSiteMapping
        {
            AssetId = request.AssetId,
            CustomerSiteId = request.CustomerSiteId,
            CommissionedDate = request.CommissionedDate,
            MappedOnUtc = clock.UtcNow,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };
        db.AssetSiteMappings.Add(mapping);

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

        return new MapAssetToSiteResponse(mapping.Id, mapping.AssetId, mapping.CustomerSiteId);
    }
}
