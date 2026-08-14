using AMS.Modules.Discovery.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Discovery.Features.SearchInstalledSoftware;

/// <summary>
/// What is installed, and whether we are licensed for it. Catalogue: Installed
/// Software.
/// </summary>
/// <remarks>
/// <para>
/// One row per TITLE, not per installation. The question this screen answers is
/// "are we compliant", and that is asked about a title across the estate; the
/// per-machine list is a filter on it.
/// </para>
/// <para>
/// A title nobody has catalogued is <c>IsInCatalogue = false</c>, which is not
/// the same as unlicensed. It means undecided, and showing the two the same way
/// would make every new title look like a breach — which is how a compliance
/// screen stops being read.
/// </para>
/// </remarks>
public sealed class SearchInstalledSoftwareHandler(DiscoveryDbContext db)
    : IRequestHandler<SearchInstalledSoftwareQuery, SearchInstalledSoftwareResponse>
{
    public async Task<Result<SearchInstalledSoftwareResponse>> HandleAsync(
        SearchInstalledSoftwareQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var installs = db.AssetInstalledSoftwares.AsNoTracking();

        if (!request.IncludeRemoved)
        {
            installs = installs.Where(s => !s.IsRemoved);
        }

        if (request.AssetId is { } assetId)
        {
            installs = installs.Where(s => s.AssetId == assetId);
        }

        if (request.Search is { } search)
        {
            installs = installs.Where(s =>
                s.SoftwareName.Contains(search)
                || (s.Publisher != null && s.Publisher.Contains(search)));
        }

        // Counted by DISTINCT machine, not by row: two versions of the same
        // title on one laptop is one seat, and counting rows would make every
        // upgrade look like a licence breach.
        var titles = await installs
            .GroupBy(s => s.SoftwareName)
            .Select(g => new
            {
                SoftwareName = g.Key,
                Publisher = g.Max(s => s.Publisher),
                InstalledCount = g.Select(s => s.AssetId).Distinct().Count(),
            })
            .ToListAsync(ct);

        var names = titles.ConvertAll(t => t.SoftwareName);

        var catalogue = await db.SoftwareCatalogs
            .AsNoTracking()
            .Where(c => names.Contains(c.SoftwareName))
            .ToDictionaryAsync(c => c.SoftwareName, ct);

        var rows = new List<SearchInstalledSoftwareResponse.Row>(titles.Count);

        foreach (var title in titles)
        {
            catalogue.TryGetValue(title.SoftwareName, out var entry);

            var overLicensed = entry?.LicensedSeats is { } seats
                && title.InstalledCount > seats;

            if (request.BlacklistedOnly && entry?.IsBlacklisted != true)
            {
                continue;
            }

            if (request.OverLicensedOnly && !overLicensed)
            {
                continue;
            }

            rows.Add(new SearchInstalledSoftwareResponse.Row(
                title.SoftwareName,
                title.Publisher,
                title.InstalledCount,
                entry?.LicensedSeats,
                overLicensed,
                entry?.IsBlacklisted ?? false,
                entry is not null,
                entry?.ContractId));
        }

        rows = [.. rows.OrderByDescending(r => r.InstalledCount).ThenBy(r => r.SoftwareName)];

        return new SearchInstalledSoftwareResponse(
            rows,
            rows.Where(r => r.IsBlacklisted).Sum(r => r.InstalledCount),
            rows.Count(r => r.IsOverLicensed));
    }
}
