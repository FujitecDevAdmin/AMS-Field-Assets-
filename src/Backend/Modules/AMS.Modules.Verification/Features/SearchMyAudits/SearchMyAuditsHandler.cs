using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.Modules.Verification.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Features.SearchMyAudits;

public sealed class SearchMyAuditsHandler(
    VerificationDbContext db,
    ICurrentUser currentUser,
    IBranchDirectory branches,
    IAssetSnapshot assets)
    : IRequestHandler<SearchMyAuditsQuery, SearchMyAuditsResponse>
{
    public async Task<Result<SearchMyAuditsResponse>> HandleAsync(
        SearchMyAuditsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cycles = await db.PhysicalVerificationAssignments.AsNoTracking()
            .Where(assignment => assignment.AuditorUserId == currentUser.Id)
            .Join(db.PhysicalVerificationCycles.AsNoTracking(),
                assignment => assignment.PhysicalVerificationCycleId,
                cycle => cycle.Id,
                (_, cycle) => cycle)
            .OrderByDescending(cycle => cycle.StartDate)
            .ThenByDescending(cycle => cycle.Id)
            .ToListAsync(ct);

        var rows = new List<SearchMyAuditsResponse.AuditRow>(cycles.Count);
        foreach (var cycle in cycles)
        {
            var locationIds = await db.PhysicalVerificationCycleLocations.AsNoTracking()
                .Where(location => location.PhysicalVerificationCycleId == cycle.Id)
                .Select(location => location.BranchId)
                .ToListAsync(ct);
            if (locationIds.Count == 0)
            {
                locationIds.Add(cycle.BranchId);
            }
            var selectedBranches = await branches.FindActiveAsync(locationIds, ct);
            var aliases = selectedBranches
                .SelectMany(branch => new[] { branch.BranchCode, branch.BranchName })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var scopedAssets = await assets.ListByImportedBranchesAsync(locationIds, aliases, ct);
            var verifiedIds = await db.PhysicalVerifications.AsNoTracking()
                .Where(verification => verification.PhysicalVerificationCycleId == cycle.Id)
                .Select(verification => verification.AssetId)
                .ToHashSetAsync(ct);
            var branchName = selectedBranches.FirstOrDefault(branch => branch.Id == cycle.BranchId)?.BranchName
                ?? (selectedBranches.Count > 0 ? selectedBranches[0].BranchName : $"Branch {cycle.BranchId}");

            rows.Add(new SearchMyAuditsResponse.AuditRow(
                cycle.Id,
                cycle.CycleName,
                cycle.BranchId,
                branchName,
                cycle.StartDate,
                cycle.EndDate,
                cycle.IsActive,
                scopedAssets.Select(asset => new SearchMyAuditsResponse.AssetRow(
                    asset.AssetId,
                    asset.AssetNumber,
                    asset.AssetName ?? asset.AssetNumber,
                    asset.SerialNumber,
                    asset.QrCodeValue,
                    asset.BarcodeValue,
                    asset.ImportedLocation,
                    asset.Quantity,
                    asset.IsBulk,
                    verifiedIds.Contains(asset.AssetId))).ToArray()));
        }

        return new SearchMyAuditsResponse(rows);
    }
}
