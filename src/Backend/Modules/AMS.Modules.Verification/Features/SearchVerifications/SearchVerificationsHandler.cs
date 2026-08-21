using AMS.Modules.Verification.Domain;
using AMS.Modules.Verification.Persistence;
using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Identity.PublicApi.Identity;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Features.SearchVerifications;

/// <summary>
/// What was found, and what was not. Catalogue: the exception report.
/// </summary>
/// <remarks>
/// Ordered worst first — Missing above NotWorking above Damaged — because the
/// screen exists to be acted on, and a report that buries the missing assets
/// under three hundred healthy ones is a report nobody finishes reading.
/// </remarks>
public sealed class SearchVerificationsHandler(
    VerificationDbContext db,
    IAssetSnapshot? assets = null,
    IBranchDirectory? branches = null,
    IUserDirectory? users = null)
    : IRequestHandler<SearchVerificationsQuery, SearchVerificationsResponse>
{
    public async Task<Result<SearchVerificationsResponse>> HandleAsync(
        SearchVerificationsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.PhysicalVerifications.AsNoTracking();

        if (request.CycleId is { } cycleId)
        {
            query = query.Where(v => v.PhysicalVerificationCycleId == cycleId);
        }

        if (request.LocationId is { } locationId)
        {
            query = query.Where(v => v.LocationId == locationId);
        }

        if (request.WorkingCondition is { } condition)
        {
            query = query.Where(v => v.WorkingCondition == condition);
        }

        if (request.ExceptionsOnly)
        {
            query = query.Where(v => v.WorkingCondition != WorkingCondition.Good);
        }

        if (request.MismatchesOnly)
        {
            query = query.Where(v => v.HasQrMismatch);
        }

        if (request.BranchId is { } branchId)
        {
            var cycleIds = db.PhysicalVerificationCycles.AsNoTracking()
                .Where(cycle => cycle.BranchId == branchId)
                .Select(cycle => cycle.Id);
            query = query.Where(item => cycleIds.Contains(item.PhysicalVerificationCycleId));
        }

        var candidates = await query
            .OrderBy(v =>
                v.WorkingCondition == WorkingCondition.Missing ? 0
                : v.WorkingCondition == WorkingCondition.NotWorking ? 1
                : v.WorkingCondition == WorkingCondition.Damaged ? 2
                : v.WorkingCondition == WorkingCondition.MinorDamage ? 3
                : 4)
            // A tag on the wrong asset is its own kind of wrong, and it can
            // happen to something in perfect condition.
            .ThenByDescending(v => v.HasQrMismatch)
            .ThenBy(v => v.AssetId)
            .ToListAsync(ct);

        var cycleIdsForRows = candidates.Select(item => item.PhysicalVerificationCycleId).Distinct().ToArray();
        var cycles = await db.PhysicalVerificationCycles.AsNoTracking()
            .Where(cycle => cycleIdsForRows.Contains(cycle.Id))
            .ToDictionaryAsync(cycle => cycle.Id, ct);
        IReadOnlyList<AssetSnapshot> assetRows = assets is null
            ? Array.Empty<AssetSnapshot>()
            : await assets.GetManyAsync(candidates.Select(item => item.AssetId).Distinct().ToArray(), ct);
        var assetById = assetRows.ToDictionary(item => item.AssetId);
        var branchIds = cycles.Values.Select(cycle => cycle.BranchId).Distinct().ToArray();
        IReadOnlyList<BranchReference> branchRows = branches is null
            ? Array.Empty<BranchReference>()
            : await branches.FindActiveAsync(branchIds, ct);
        var branchById = branchRows.ToDictionary(item => item.Id);
        var userById = new Dictionary<int, UserContact?>();
        if (users is not null)
        {
            foreach (var userId in candidates.Select(item => item.VerifiedByUserId).Distinct())
            {
                userById[userId] = await users.FindAsync(userId, ct);
            }
        }

        var enriched = candidates.Select(v =>
        {
            cycles.TryGetValue(v.PhysicalVerificationCycleId, out var cycle);
            assetById.TryGetValue(v.AssetId, out var asset);
            var cycleBranchId = cycle?.BranchId;
            var branchName = cycleBranchId is { } id && branchById.TryGetValue(id, out var branch)
                ? branch.BranchName
                : asset?.ImportedBranch;
            userById.TryGetValue(v.VerifiedByUserId, out var auditor);
            return new SearchVerificationsResponse.Row(
                v.Id, v.PhysicalVerificationCycleId, v.AssetId, v.IsBulkCount,
                v.CountedQuantity, v.ExpectedQuantitySnapshot,
                v.CountedQuantity != null && v.ExpectedQuantitySnapshot != null
                    ? v.CountedQuantity - v.ExpectedQuantitySnapshot : null,
                v.WorkingCondition, v.HasQrMismatch, v.SerialVerified, v.LocationId,
                v.HolderEmployeeId, v.GpsLatitude, v.GpsLongitude, v.PhotoPath,
                v.VerifiedByUserId, AsUtc(v.VerifiedOnUtc), v.Remarks,
                cycle?.CycleName, asset?.AssetNumber, asset?.AssetName, cycleBranchId,
                branchName, asset?.ImportedLocation, auditor?.DisplayName ?? v.CreatedBy,
                v.ScannedQrValue);
        });

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            enriched = enriched.Where(row => string.Equals(
                row.LocationName, request.Location, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search;
            enriched = enriched.Where(row => new[]
            {
                row.AssetNumber, row.AssetName, row.AuditName, row.BranchName,
                row.LocationName, row.AuditorName, row.ScannedQrValue, row.Remarks,
            }.Any(value => value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true));
        }

        var filtered = enriched.ToList();
        var total = filtered.Count;
        var exceptions = filtered.Count(row => row.WorkingCondition != WorkingCondition.Good);
        var rows = filtered.Skip(request.Skip).Take(request.Take).ToList();

        return new SearchVerificationsResponse(rows, total, exceptions);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc
        ? value
        : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
