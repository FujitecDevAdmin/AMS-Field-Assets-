using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.SearchUsers;

/// <summary>
/// The Users grid. Catalogue screen: Users.
/// </summary>
/// <remarks>
/// Paged at the database, never in memory: the count and the page are two
/// queries over the same filter, and an unbounded <c>ToListAsync</c> on a
/// business table is a review-blocker (02 §8).
/// </remarks>
public sealed class SearchUsersHandler(IdentityDbContext db)
    : IRequestHandler<SearchUsersQuery, SearchUsersResponse>
{
    public async Task<Result<SearchUsersResponse>> HandleAsync(SearchUsersQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search}%";
            query = query.Where(u => EF.Functions.Like(u.Username, term)
                                  || EF.Functions.Like(u.DisplayName, term)
                                  || (u.Email != null && EF.Functions.Like(u.Email, term)));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(u => u.Username)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(u => new SearchUsersResponse.Row(
                u.Id,
                u.Username,
                u.DisplayName,
                u.Email,
                u.IsActive,
                u.IsLocked,
                u.MfaEnabled,
                u.LastLoginOnUtc))
            .ToListAsync(ct);

        return new SearchUsersResponse(rows, total);
    }
}
