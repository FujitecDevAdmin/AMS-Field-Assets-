using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.SearchLocations;

/// <summary>The branch list. Catalogue screen: Branches.</summary>
/// <remarks>
/// Region is joined here rather than fetched per row: the grid shows the region
/// name, and one query per branch is the N+1 that 03 §6 calls a blocker.
/// </remarks>
public sealed class SearchLocationsHandler(OrganizationDbContext db)
    : IRequestHandler<SearchLocationsQuery, SearchLocationsResponse>
{
    public async Task<Result<SearchLocationsResponse>> HandleAsync(
        SearchLocationsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Locations.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(l => l.IsActive == request.IsActive.Value);
        }

        if (request.RegionId.HasValue)
        {
            query = query.Where(l => l.RegionId == request.RegionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search}%";
            query = query.Where(l => EF.Functions.Like(l.LocationName, term)
                                  || EF.Functions.Like(l.LocationCode, term));
        }

        var rows = await query
            .OrderBy(l => l.LocationCode)
            .Select(l => new SearchLocationsResponse.Row(
                l.Id,
                l.LocationCode,
                l.LocationName,
                l.RegionId,
                db.Regions.Where(r => r.Id == l.RegionId).Select(r => r.RegionName).FirstOrDefault(),
                l.TimeZoneId,
                l.IsHeadOffice,
                l.IsActive))
            .ToListAsync(ct);

        return new SearchLocationsResponse(rows);
    }
}
