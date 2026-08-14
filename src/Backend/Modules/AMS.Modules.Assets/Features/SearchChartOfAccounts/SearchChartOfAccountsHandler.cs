using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.SearchChartOfAccounts;

/// <summary>The ledger codes an asset's finance record points at.</summary>
public sealed class SearchChartOfAccountsHandler(AssetsDbContext db)
    : IRequestHandler<SearchChartOfAccountsQuery, SearchChartOfAccountsResponse>
{
    public async Task<Result<SearchChartOfAccountsResponse>> HandleAsync(
        SearchChartOfAccountsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.ChartOfAccounts.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == request.IsActive.Value);
        }

        var rows = await query
            .OrderBy(c => c.CoaCode)
            .Select(c => new SearchChartOfAccountsResponse.Row(
                c.Id, c.CoaCode, c.Description, c.IsActive))
            .ToListAsync(ct);

        return new SearchChartOfAccountsResponse(rows);
    }
}
