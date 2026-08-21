using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.Modules.Verification.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Features.SearchAuditAssets;

public sealed class SearchAuditAssetsHandler(
    VerificationDbContext db,
    IBranchDirectory branches,
    IAssetSnapshot assets)
    : IRequestHandler<SearchAuditAssetsQuery, SearchAuditAssetsResponse>
{
    public async Task<Result<SearchAuditAssetsResponse>> HandleAsync(
        SearchAuditAssetsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cycle = await db.PhysicalVerificationCycles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.AuditId, ct);
        if (cycle is null)
        {
            return Error.NotFound("PhysicalVerificationCycle", request.AuditId);
        }

        var branchIds = await db.PhysicalVerificationCycleLocations.AsNoTracking()
            .Where(item => item.PhysicalVerificationCycleId == cycle.Id)
            .Select(item => item.BranchId)
            .ToListAsync(ct);
        if (branchIds.Count == 0)
        {
            branchIds.Add(cycle.BranchId);
        }

        var selectedBranches = await branches.FindActiveAsync(branchIds, ct);
        var aliases = selectedBranches
            .SelectMany(branch => new[] { branch.BranchCode, branch.BranchName })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var scopedAssets = await assets.ListByImportedBranchesAsync(branchIds, aliases, ct);
        var verificationRows = await db.PhysicalVerifications.AsNoTracking()
            .Where(item => item.PhysicalVerificationCycleId == cycle.Id)
            .OrderByDescending(item => item.VerifiedOnUtc)
            .Select(item => new
            {
                item.AssetId,
                item.VerifiedByUserId,
                VerifiedBy = item.CreatedBy,
                item.VerifiedOnUtc,
            })
            .ToListAsync(ct);
        var latestVerificationByAsset = verificationRows
            .GroupBy(item => item.AssetId)
            .ToDictionary(group => group.Key, group => group.First());
        var ownerBranch = selectedBranches.FirstOrDefault(branch => branch.Id == cycle.BranchId);
        var branchName = ownerBranch?.BranchName
            ?? (selectedBranches.Count > 0 ? selectedBranches[0].BranchName : $"Branch {cycle.BranchId}");

        return new SearchAuditAssetsResponse(
            cycle.Id,
            cycle.CycleName,
            branchName,
            cycle.IsActive ? "Active" : "Closed",
            scopedAssets.Select(asset =>
            {
                latestVerificationByAsset.TryGetValue(asset.AssetId, out var verification);
                return new SearchAuditAssetsResponse.AssetRow(
                    asset.AssetId,
                    asset.AssetNumber,
                    asset.AssetName ?? asset.AssetNumber,
                    asset.SerialNumber,
                    asset.ImportedLocation,
                    asset.Quantity,
                    verification is not null,
                    verification?.VerifiedByUserId,
                    verification?.VerifiedBy,
                    verification is null ? null : AsUtc(verification.VerifiedOnUtc));
            }).ToArray());
    }

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc
        ? value
        : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
