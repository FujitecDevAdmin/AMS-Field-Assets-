using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.SearchApplications;

/// <summary>The application master. Catalogue screen: Applications and Access.</summary>
public sealed class SearchApplicationsHandler(OrganizationDbContext db)
    : IRequestHandler<SearchApplicationsQuery, SearchApplicationsResponse>
{
    public async Task<Result<SearchApplicationsResponse>> HandleAsync(
        SearchApplicationsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Applications.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(a => a.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search}%";
            query = query.Where(a => EF.Functions.Like(a.ApplicationName, term));
        }

        var rows = await query
            .OrderBy(a => a.ApplicationName)
            .Select(a => new SearchApplicationsResponse.Row(
                a.Id,
                a.ApplicationName,
                a.IsActive,

                // Current holders only: a revoked grant is history, not access.
                db.EmployeeApplications.Count(ea => ea.ApplicationId == a.Id && ea.RevokedOnUtc == null)))
            .ToListAsync(ct);

        return new SearchApplicationsResponse(rows);
    }
}
