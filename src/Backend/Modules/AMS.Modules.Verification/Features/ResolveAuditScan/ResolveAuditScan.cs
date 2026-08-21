using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.Modules.Verification.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Features.ResolveAuditScan;

public sealed record ResolveAuditScanRequest(string ScanCode);

public sealed record ResolveAuditScanQuery(int AuditId, string ScanCode)
    : IQuery<ResolveAuditScanResponse>;

public sealed record ResolveAuditScanResponse(
    int Id,
    string AssetNumber,
    string AssetName,
    string? SerialNumber,
    string? QrCodeValue,
    string? BarcodeValue,
    string? Location,
    decimal Quantity,
    bool IsBulk,
    bool IsVerified);

public static class ResolveAuditScanEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapPost("/my-audits/{auditId:int}/resolve-scan", async (
                int auditId,
                ResolveAuditScanRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var result = await dispatcher.SendAsync(
                    new ResolveAuditScanQuery(auditId, request.ScanCode), ct);
                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Verification.Run)
            .WithName("ResolveAuditScan")
            .Produces<ResolveAuditScanResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
}

public sealed class ResolveAuditScanHandler(
    VerificationDbContext db,
    ICurrentUser currentUser,
    IBranchDirectory branches,
    IAssetSnapshot assets)
    : IRequestHandler<ResolveAuditScanQuery, ResolveAuditScanResponse>
{
    public async Task<Result<ResolveAuditScanResponse>> HandleAsync(
        ResolveAuditScanQuery request,
        CancellationToken ct)
    {
        if (request.AuditId <= 0 || string.IsNullOrWhiteSpace(request.ScanCode))
        {
            return Error.Validation("Verification.InvalidScan", "Scan an asset code, QR code, or barcode.");
        }

        var isAssigned = await db.PhysicalVerificationAssignments.AsNoTracking()
            .AnyAsync(assignment =>
                assignment.PhysicalVerificationCycleId == request.AuditId
                && assignment.AuditorUserId == currentUser.Id, ct);
        if (!isAssigned)
        {
            return Error.Forbidden(
                "VerificationCycle.NotAssigned",
                "You are not assigned to conduct this audit.");
        }

        var cycle = await db.PhysicalVerificationCycles.AsNoTracking()
            .SingleOrDefaultAsync(cycle => cycle.Id == request.AuditId && cycle.IsActive, ct);
        if (cycle is null)
        {
            return Error.Validation("VerificationCycle.NotActive", "The selected audit is not active.");
        }

        var asset = await assets.FindByScanCodeAsync(request.ScanCode, ct);
        if (asset is null)
        {
            return Error.NotFound("AssetScan", request.ScanCode.Trim());
        }

        var branchIds = await db.PhysicalVerificationCycleLocations.AsNoTracking()
            .Where(scope => scope.PhysicalVerificationCycleId == request.AuditId)
            .Select(scope => scope.BranchId)
            .ToListAsync(ct);
        if (branchIds.Count == 0)
        {
            branchIds.Add(cycle.BranchId);
        }
        var branchReferences = await branches.FindActiveAsync(branchIds, ct);
        var branchAliases = branchReferences
            .SelectMany(branch => new[] { branch.BranchCode, branch.BranchName })
            .Select(Normalize)
            .ToHashSet(StringComparer.Ordinal);

        // CurrentLocationId and audit BranchId are the same Organization.Branch key.
        // Text comparison exists only for imported legacy assets that have no ID yet.
        var isInScope = asset.CurrentLocationId is { } locationId
            ? branchIds.Contains(locationId)
            : branchAliases.Contains(Normalize(asset.ImportedBranch));
        if (!isInScope)
        {
            return Error.Forbidden(
                "Verification.AssetOutsideAuditBranch",
                "This asset belongs to a different branch or is outside this audit's scope.");
        }

        var isVerified = await db.PhysicalVerifications.AsNoTracking()
            .AnyAsync(verification =>
                verification.PhysicalVerificationCycleId == request.AuditId
                && verification.AssetId == asset.AssetId, ct);

        return new ResolveAuditScanResponse(
            asset.AssetId,
            asset.AssetNumber,
            asset.AssetName ?? asset.AssetNumber,
            asset.SerialNumber,
            asset.QrCodeValue,
            asset.BarcodeValue,
            asset.ImportedLocation,
            asset.Quantity,
            asset.IsBulk,
            isVerified);
    }

    private static string Normalize(string? value) => string.Concat(
        (value ?? string.Empty).Where(char.IsLetterOrDigit)).ToUpperInvariant();
}
