using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.SearchRegions;

/// <summary>
/// The region list. Catalogue: Regions — "a master list such as North and
/// South, used to route tickets to the right support team".
/// </summary>
public sealed class SearchRegionsHandler(OrganizationDbContext db)
    : IRequestHandler<SearchRegionsQuery, SearchRegionsResponse>
{
    public async Task<Result<SearchRegionsResponse>> HandleAsync(SearchRegionsQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Regions.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search}%";
            query = query.Where(r => EF.Functions.Like(r.RegionName, term));
        }

        var rows = await query
            .OrderBy(r => r.RegionName)
            .Select(r => new SearchRegionsResponse.Row(
                r.Id,
                r.RegionName,
                r.Description,
                r.IsActive,
                db.Locations.Count(l => l.RegionId == r.Id)))
            .ToListAsync(ct);

        return new SearchRegionsResponse(rows);
    }
}
