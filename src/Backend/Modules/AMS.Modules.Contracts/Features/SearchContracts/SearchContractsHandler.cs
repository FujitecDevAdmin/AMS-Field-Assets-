using AMS.Modules.Contracts.Persistence;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Contracts.Features.SearchContracts;

/// <summary>
/// Contracts, and the ones about to run out. Catalogue: Contracts.
/// </summary>
/// <remarks>
/// Ordered soonest to expire, because the screen exists to answer one question:
/// what do I have to do something about. A contract that ran out last year is
/// still findable, but it is not what anybody opened the page for.
/// </remarks>
public sealed class SearchContractsHandler(
    ContractsDbContext db,
    IVendorDirectory vendors,
    IClock clock)
    : IRequestHandler<SearchContractsQuery, SearchContractsResponse>
{
    public async Task<Result<SearchContractsResponse>> HandleAsync(
        SearchContractsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var today = DateOnly.FromDateTime(clock.UtcNow);

        // Soft-deleted contracts are gone as far as every screen is concerned.
        // Nothing is removed, because a contract that covered an asset last
        // year is what explains why a repair was free.
        var live = db.Contracts.AsNoTracking().Where(c => !c.IsDeleted);

        var expiring = await live.CountAsync(
            c => c.EndDate >= today && c.EndDate <= today.AddDays(30), ct);

        var query = live;

        if (!request.IncludeExpired)
        {
            query = query.Where(c => c.EndDate >= today);
        }

        if (request.Search is { } search)
        {
            query = query.Where(c =>
                c.ContractNumber.Contains(search) || c.ContractName.Contains(search));
        }

        if (request.ContractType is { } type)
        {
            query = query.Where(c => c.ContractType == type);
        }

        if (request.VendorId is { } vendorId)
        {
            query = query.Where(c => c.VendorId == vendorId);
        }

        if (request.ExpiringWithinDays is { } within)
        {
            query = query.Where(c => c.EndDate >= today && c.EndDate <= today.AddDays(within));
        }

        var total = await query.CountAsync(ct);

        var page = await query
            .OrderBy(c => c.EndDate)
            .ThenBy(c => c.ContractNumber)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(c => new
            {
                c.Id,
                c.ContractNumber,
                c.ContractName,
                c.ContractType,
                c.VendorId,
                c.StartDate,
                c.EndDate,
                c.ContractValue,
                c.AutoRenew,
                AssetCount = db.ContractAssets.Count(a => a.ContractId == c.Id),
            })
            .ToListAsync(ct);

        var rows = new List<SearchContractsResponse.Row>(page.Count);

        foreach (var c in page)
        {
            // One lookup per DISTINCT vendor would be tidier; one per row is
            // what a page of fifty costs, and the directory is a dictionary
            // read behind an interface. Worth revisiting if it ever isn't.
            var vendor = c.VendorId is { } id ? await vendors.FindAsync(id, ct) : null;

            rows.Add(new SearchContractsResponse.Row(
                c.Id,
                c.ContractNumber,
                c.ContractName,
                c.ContractType,
                c.VendorId,
                vendor?.VendorName,
                c.StartDate,
                c.EndDate,
                c.EndDate.DayNumber - today.DayNumber,
                c.ContractValue,
                c.AutoRenew,
                c.AssetCount));
        }

        return new SearchContractsResponse(rows, total, expiring);
    }
}
