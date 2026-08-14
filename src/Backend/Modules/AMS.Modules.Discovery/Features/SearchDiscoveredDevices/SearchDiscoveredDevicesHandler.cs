using AMS.Modules.Discovery.Domain;
using AMS.Modules.Discovery.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Discovery.Features.SearchDiscoveredDevices;

/// <summary>Machines the agent has found. Catalogue: Discovered Devices.</summary>
/// <remarks>
/// The unresolved count is over everything, not the filter. It is the queue
/// length — the number that says whether anybody is keeping up — and a reader
/// who has filtered to one hostname still wants it.
/// </remarks>
public sealed class SearchDiscoveredDevicesHandler(DiscoveryDbContext db)
    : IRequestHandler<SearchDiscoveredDevicesQuery, SearchDiscoveredDevicesResponse>
{
    public async Task<Result<SearchDiscoveredDevicesResponse>> HandleAsync(
        SearchDiscoveredDevicesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var all = db.DiscoveredDevices.AsNoTracking();

        var unresolved = await all.CountAsync(
            d => d.Status == DiscoveredDeviceStatus.New, ct);

        var query = all;

        if (request.UnresolvedOnly)
        {
            query = query.Where(d => d.Status == DiscoveredDeviceStatus.New);
        }

        if (request.Status is { } status)
        {
            query = query.Where(d => d.Status == status);
        }

        if (request.Search is { } search)
        {
            query = query.Where(d =>
                d.Hostname.Contains(search)
                || (d.SerialNumber != null && d.SerialNumber.Contains(search))
                || (d.Model != null && d.Model.Contains(search)));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(d => d.LastSeenOnUtc)
            .ThenByDescending(d => d.Id)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(d => new SearchDiscoveredDevicesResponse.Row(
                d.Id, d.Hostname, d.SerialNumber, d.Manufacturer, d.Model,
                d.OperatingSystem, d.MacAddress, d.Status, d.LinkedAssetId,
                d.FirstSeenOnUtc, d.LastSeenOnUtc))
            .ToListAsync(ct);

        return new SearchDiscoveredDevicesResponse(rows, total, unresolved);
    }
}
