using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.SearchCustomerSites;

/// <summary>The site master. Catalogue screen: Customer Sites.</summary>
/// <remarks>
/// Not paged: a customer site list is tens of rows, and the screen is a picker
/// as often as it is a grid.
/// </remarks>
public sealed class SearchCustomerSitesHandler(AllocationsDbContext db)
    : IRequestHandler<SearchCustomerSitesQuery, SearchCustomerSitesResponse>
{
    public async Task<Result<SearchCustomerSitesResponse>> HandleAsync(
        SearchCustomerSitesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.CustomerSites.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search}%";
            query = query.Where(s => EF.Functions.Like(s.SiteName, term)
                                  || (s.CustomerName != null && EF.Functions.Like(s.CustomerName, term))
                                  || (s.City != null && EF.Functions.Like(s.City, term)));
        }

        var rows = await query
            .OrderBy(s => s.CustomerName)
            .ThenBy(s => s.SiteName)
            .Select(s => new SearchCustomerSitesResponse.Row(
                s.Id,
                s.CustomerName,
                s.SiteName,
                s.City,
                s.Address,
                s.Latitude,
                s.Longitude,
                s.IsActive,
                // Only live mappings: a site showing assets that left last year
                // is a site nobody trusts the count on.
                db.AssetSiteMappings.Count(m => m.CustomerSiteId == s.Id && m.RemovedOnUtc == null)))
            .ToListAsync(ct);

        return new SearchCustomerSitesResponse(rows);
    }
}
