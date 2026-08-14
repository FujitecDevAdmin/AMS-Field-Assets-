using AMS.Modules.Discovery.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Discovery.Features.SearchAgentKeys;

/// <summary>The agent keys. Catalogue: Agent Keys.</summary>
/// <remarks>
/// The hash is never projected. There would be nothing to do with it — it
/// cannot be turned back into a key — but a hash on a screen is a hash in a
/// screenshot, and the column exists to be compared against, not read.
/// </remarks>
public sealed class SearchAgentKeysHandler(DiscoveryDbContext db)
    : IRequestHandler<SearchAgentKeysQuery, SearchAgentKeysResponse>
{
    public async Task<Result<SearchAgentKeysResponse>> HandleAsync(
        SearchAgentKeysQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.AgentApiKeys.AsNoTracking();

        if (request.ActiveOnly)
        {
            query = query.Where(k => k.IsActive);
        }

        var rows = await query
            .OrderByDescending(k => k.LastUsedOnUtc ?? k.CreatedOnUtc)
            .Select(k => new SearchAgentKeysResponse.Row(
                k.Id, k.KeyName, k.KeyPrefix, k.IsActive,
                k.LastUsedOnUtc, k.RevokedOnUtc, k.CreatedOnUtc))
            .ToListAsync(ct);

        return new SearchAgentKeysResponse(rows);
    }
}
