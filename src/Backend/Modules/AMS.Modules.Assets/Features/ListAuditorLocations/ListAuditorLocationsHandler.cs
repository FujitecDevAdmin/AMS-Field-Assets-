using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.ListAuditorLocations;

public sealed class ListAuditorLocationsHandler(AssetsDbContext db)
    : IRequestHandler<ListAuditorLocationsQuery, ListAuditorLocationsResponse>
{
    internal const string RegistryTypeName = "Auditor Assigned Locations";

    public async Task<Result<ListAuditorLocationsResponse>> HandleAsync(
        ListAuditorLocationsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var registryTypeId = await db.AssetTypes
            .Where(type => type.TypeName == RegistryTypeName)
            .Select(type => (int?)type.Id)
            .SingleOrDefaultAsync(ct);

        if (registryTypeId is null)
        {
            return new ListAuditorLocationsResponse([]);
        }

        var rows = await db.CustomFieldDefinitions
            .AsNoTracking()
            .Where(field => field.AssetTypeId == registryTypeId && field.IsActive)
            .OrderBy(field => field.DisplayOrder)
            .ThenBy(field => field.DisplayLabel)
            .Select(field => new ListAuditorLocationsResponse.Row(
                field.Id,
                field.DisplayOrder,
                field.DisplayLabel))
            .ToListAsync(ct);

        return new ListAuditorLocationsResponse(rows);
    }
}
