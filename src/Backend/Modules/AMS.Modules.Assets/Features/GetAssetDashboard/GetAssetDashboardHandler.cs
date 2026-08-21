using System.Globalization;
using System.Text.Json;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.GetAssetDashboard;

public sealed class GetAssetDashboardHandler(AssetsDbContext db, ICurrentUser currentUser, IClock clock)
    : IRequestHandler<GetAssetDashboardQuery, GetAssetDashboardResponse>
{
    private static readonly string[] ValueFields = ["Current Gross Value", "Net Book Value", "Gross Value", "Orignal Value", "Original Value"];
    private static readonly string[] LocationFields = ["Location", "Branch", "SAP Plant"];
    private static readonly string[] DepartmentFields = ["Department", "TechnicalGroup", "Technical Group", "Cost Centre"];

    public async Task<Result<GetAssetDashboardResponse>> HandleAsync(
        GetAssetDashboardQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Assets.AsNoTracking().Where(asset => !asset.IsDeleted);
        if (!currentUser.HasAllBranches)
        {
            var branches = currentUser.BranchIds;
            query = query.Where(asset => asset.CurrentLocationId == null
                || branches.Contains(asset.CurrentLocationId.Value));
        }

        var rows = await query
            .Select(asset => new DashboardRow(
                asset.Id,
                asset.AssetNumber,
                asset.AssetName,
                db.AssetStatuses.Where(status => status.Id == asset.AssetStatusId)
                    .Select(status => status.StatusName).First(),
                db.AssetTypes.Where(type => type.Id == asset.AssetTypeId)
                    .Select(type => type.TypeName).First(),
                asset.CurrentLocationId,
                asset.CurrentEmployeeId,
                asset.DepartmentId,
                asset.CostCenter,
                asset.LastPhysicalCheckOnUtc,
                asset.CreatedOnUtc,
                asset.ImportedDataJson))
            .ToListAsync(ct);

        // PhysicalVerification is the source of truth. LastPhysicalCheckOnUtc is
        // maintained as a fast asset snapshot, but older mobile captures predate
        // that synchronization and must still appear in analytics.
        var verificationDates = await db.Database
            .SqlQueryRaw<DashboardVerificationRow>(
                """
                SELECT [AssetId], MAX([VerifiedOnUtc]) AS [LastVerifiedOnUtc]
                FROM [Verification].[PhysicalVerification]
                GROUP BY [AssetId]
                """)
            .ToListAsync(ct);
        var latestVerificationByAsset = verificationDates
            .ToDictionary(item => item.AssetId, item => item.LastVerifiedOnUtc);
        rows = rows.Select(row => latestVerificationByAsset.TryGetValue(row.Id, out var verifiedOn)
                && (!row.LastPhysicalCheckOnUtc.HasValue || verifiedOn > row.LastPhysicalCheckOnUtc)
            ? row with { LastPhysicalCheckOnUtc = verifiedOn }
            : row).ToList();

        var enriched = rows.Select(Enrich).ToList();
        var statusBreakdown = enriched.GroupBy(row => row.Status, StringComparer.OrdinalIgnoreCase)
            .Select(group => new AssetDashboardBreakdown(group.Key, group.Count(), group.Count()))
            .OrderByDescending(item => item.Count).ToList();
        var typeBreakdown = enriched.GroupBy(row => row.Type, StringComparer.OrdinalIgnoreCase)
            .Select(group => new AssetDashboardBreakdown(group.Key, group.Count(), group.Count()))
            .OrderByDescending(item => item.Count).Take(8).ToList();

        return new GetAssetDashboardResponse(
            enriched.Count,
            enriched.Count(row => row.LastPhysicalCheckOnUtc.HasValue),
            enriched.Count(row => !row.LastPhysicalCheckOnUtc.HasValue),
            enriched.Count(row => Contains(row.Status, "missing") || Contains(row.Status, "lost")),
            enriched.Count(row => row.CurrentEmployeeId.HasValue),
            enriched.Count(row => !row.CurrentEmployeeId.HasValue),
            enriched.Count(row => Contains(row.Status, "repair") || Contains(row.Status, "maintenance")),
            enriched.Count(row => Contains(row.Status, "disposed") || Contains(row.Status, "scrap") || Contains(row.Status, "retired")),
            enriched.Sum(row => row.Value),
            clock.UtcNow,
            BuildValueBreakdown(enriched, row => row.Location),
            BuildValueBreakdown(enriched, row => row.Department),
            statusBreakdown,
            typeBreakdown,
            BuildTrend(enriched, clock.UtcNow),
            enriched.OrderByDescending(row => row.CreatedOnUtc).Take(6)
                .Select(row => new AssetDashboardRecentAsset(
                    row.Id, row.AssetNumber, row.AssetName, row.Status, row.Location, row.CreatedOnUtc))
                .ToList());
    }

    private static List<AssetDashboardBreakdown> BuildValueBreakdown(
        IEnumerable<EnrichedRow> rows,
        Func<EnrichedRow, string> selector) => rows.GroupBy(selector, StringComparer.OrdinalIgnoreCase)
            .Select(group => new AssetDashboardBreakdown(group.Key, group.Sum(row => row.Value), group.Count()))
            .OrderByDescending(item => item.Value).Take(8).ToList();

    private static List<AssetDashboardTrendPoint> BuildTrend(
        IReadOnlyList<EnrichedRow> rows,
        DateTime current)
    {
        return Enumerable.Range(0, 6).Reverse().Select(offset => new DateTime(current.Year, current.Month, 1).AddMonths(-offset))
            .Select(month => new AssetDashboardTrendPoint(
                month.ToString("MMM yy", CultureInfo.InvariantCulture),
                rows.Count(row => row.CreatedOnUtc.Year == month.Year && row.CreatedOnUtc.Month == month.Month),
                rows.Count(row => row.LastPhysicalCheckOnUtc?.Year == month.Year && row.LastPhysicalCheckOnUtc?.Month == month.Month)))
            .ToList();
    }

    private static EnrichedRow Enrich(DashboardRow row)
    {
        var imported = Deserialize(row.ImportedDataJson);
        var location = First(imported, LocationFields) ?? (row.CurrentLocationId.HasValue ? $"Location {row.CurrentLocationId}" : "Unassigned");
        var department = First(imported, DepartmentFields) ?? row.CostCenter ?? (row.DepartmentId.HasValue ? $"Department {row.DepartmentId}" : "Unassigned");
        var rawValue = First(imported, ValueFields);
        _ = decimal.TryParse(rawValue?.Replace(",", string.Empty, StringComparison.Ordinal), NumberStyles.Any, CultureInfo.InvariantCulture, out var value);
        return new EnrichedRow(row.Id, row.AssetNumber, row.AssetName, row.Status, row.Type, row.CurrentEmployeeId,
            row.LastPhysicalCheckOnUtc, row.CreatedOnUtc, location, department, value);
    }

    private static Dictionary<string, string?> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)?
                .ToDictionary(pair => pair.Key, pair => pair.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? null : pair.Value.ToString(), StringComparer.OrdinalIgnoreCase)
                ?? new(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string? First(Dictionary<string, string?> values, IEnumerable<string> names) =>
        names.Select(name => values.TryGetValue(name, out var value) ? value : null).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static bool Contains(string value, string term) => value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private sealed record DashboardRow(int Id, string AssetNumber, string AssetName, string Status, string Type,
        int? CurrentLocationId, int? CurrentEmployeeId, int? DepartmentId, string? CostCenter,
        DateTime? LastPhysicalCheckOnUtc, DateTime CreatedOnUtc, string? ImportedDataJson);

    private sealed record EnrichedRow(int Id, string AssetNumber, string AssetName, string Status, string Type,
        int? CurrentEmployeeId, DateTime? LastPhysicalCheckOnUtc, DateTime CreatedOnUtc,
        string Location, string Department, decimal Value);

    private sealed class DashboardVerificationRow
    {
        public int AssetId { get; init; }

        public DateTime LastVerifiedOnUtc { get; init; }
    }
}
