using AMS.Modules.Discovery.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Discovery.Features.SearchAssetHealth;

/// <summary>How the machines are doing. Catalogue: Asset Health.</summary>
/// <remarks>
/// Ordered by the full system drive first, because that is the reading that
/// turns into a ticket: a machine at 98 per cent stops installing updates and
/// then stops working, and it does so predictably enough to get ahead of.
/// </remarks>
public sealed class SearchAssetHealthHandler(DiscoveryDbContext db, IClock clock)
    : IRequestHandler<SearchAssetHealthQuery, SearchAssetHealthResponse>
{
    public async Task<Result<SearchAssetHealthResponse>> HandleAsync(
        SearchAssetHealthQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = clock.UtcNow;
        var query = db.AssetHealths.AsNoTracking();

        if (request.AssetId is { } assetId)
        {
            query = query.Where(h => h.AssetId == assetId);
        }

        if (request.MinDrivePercent is { } minDrive)
        {
            query = query.Where(h => h.SystemDrivePercent >= minDrive);
        }

        if (request.NotSeenForHours is { } hours)
        {
            var cutoff = now.AddHours(-hours);
            query = query.Where(h => h.LastSeenOnUtc < cutoff);
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(h => h.SystemDrivePercent)
            .ThenBy(h => h.LastSeenOnUtc)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(h => new SearchAssetHealthResponse.Row(
                h.AssetId,
                h.Hostname,
                h.CpuPercent,
                h.MemoryPercent,
                h.SystemDrivePercent,
                h.BatteryHealthPercent,
                h.UptimeHours,
                h.LoggedInUser,
                h.LastSeenOnUtc,
                (int)(now - h.LastSeenOnUtc).TotalHours))
            .ToListAsync(ct);

        return new SearchAssetHealthResponse(rows, total);
    }
}
