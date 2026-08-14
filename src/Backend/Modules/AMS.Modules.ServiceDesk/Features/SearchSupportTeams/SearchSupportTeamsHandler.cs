using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.SearchSupportTeams;

/// <summary>Teams, their members and their leads. Catalogue screen: Support Teams.</summary>
public sealed class SearchSupportTeamsHandler(ServiceDeskDbContext db)
    : IRequestHandler<SearchSupportTeamsQuery, SearchSupportTeamsResponse>
{
    public async Task<Result<SearchSupportTeamsResponse>> HandleAsync(
        SearchSupportTeamsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.SupportTeams.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == request.IsActive.Value);
        }

        if (request.RegionId.HasValue)
        {
            query = query.Where(t => t.RegionId == request.RegionId.Value);
        }

        var rows = await query
            .OrderBy(t => t.TeamName)
            .Select(t => new SearchSupportTeamsResponse.Row(
                t.Id,
                t.TeamName,
                t.RegionId,
                t.MailboxAddress,
                t.IsDefaultTeam,
                t.IsActive,
                db.SupportTeamMembers.Count(m => m.SupportTeamId == t.Id),
                db.SupportTeamMembers
                    .Where(m => m.SupportTeamId == t.Id && m.IsLead)
                    .Select(m => m.UserId)
                    .ToList()))
            .ToListAsync(ct);

        return new SearchSupportTeamsResponse(rows);
    }
}
