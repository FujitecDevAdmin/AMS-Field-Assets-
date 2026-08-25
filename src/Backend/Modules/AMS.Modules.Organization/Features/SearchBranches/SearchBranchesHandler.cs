using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.SearchBranches;

/// <summary>The branch list. Catalogue screen: Branches.</summary>
/// <remarks>
/// Region is joined here rather than fetched per row: the grid shows the region
/// name, and one query per branch is the N+1 that 03 §6 calls a blocker.
/// </remarks>
public sealed class SearchBranchesHandler(OrganizationDbContext db)
    : IRequestHandler<SearchBranchesQuery, SearchBranchesResponse>
{
    public async Task<Result<SearchBranchesResponse>> HandleAsync(
        SearchBranchesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Branches.AsNoTracking();

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
            query = query.Where(l => EF.Functions.Like(l.BranchName, term)
                                  || EF.Functions.Like(l.BranchCode, term));
        }

        var rows = await query
            .OrderBy(l => l.BranchCode)
            .Select(l => new SearchBranchesResponse.Row(
                l.Id,
                l.BranchCode,
                l.BranchName,
                l.RegionId,
                db.Regions.Where(r => r.Id == l.RegionId).Select(r => r.RegionName).FirstOrDefault(),
                l.Latitude,
                l.Longitude,
                l.TimeZoneId,
                l.IsHeadOffice,
                l.IsActive))
            .ToListAsync(ct);

        return new SearchBranchesResponse(rows);
    }
}
