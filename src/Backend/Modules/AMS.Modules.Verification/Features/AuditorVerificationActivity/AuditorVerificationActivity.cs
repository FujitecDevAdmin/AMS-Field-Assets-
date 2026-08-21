using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Verification.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Features.AuditorVerificationActivity;

public sealed record SearchAuditorVerificationCountsQuery
    : IQuery<SearchAuditorVerificationCountsResponse>;

public sealed record SearchAuditorVerificationCountsResponse(
    IReadOnlyList<SearchAuditorVerificationCountsResponse.Row> Rows)
{
    public sealed record Row(int AuditorUserId, int VerifiedAssetCount);
}

public sealed record GetAuditorVerificationActivityQuery(int AuditorUserId)
    : IQuery<GetAuditorVerificationActivityResponse>;

public sealed record GetAuditorVerificationActivityResponse(
    int AuditorUserId,
    int TotalCount,
    IReadOnlyList<GetAuditorVerificationActivityResponse.Row> Rows)
{
    public sealed record Row(
        int VerificationId,
        int AssetId,
        string AssetNumber,
        string AssetName,
        int AuditId,
        string AuditName,
        string WorkingCondition,
        DateTime VerifiedOnUtc,
        string? Remarks);
}

public static class AuditorVerificationActivityEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/auditor-verification-counts", async (
                IDispatcher dispatcher,
                CancellationToken ct) =>
            (await dispatcher.SendAsync(new SearchAuditorVerificationCountsQuery(), ct)).ToHttpResult())
            .RequireCapability(Capabilities.FieldAssets.Manage)
            .WithName("SearchAuditorVerificationCounts")
            .Produces<SearchAuditorVerificationCountsResponse>(StatusCodes.Status200OK);

        group.MapGet("/auditors/{auditorUserId:int}/verification-activity", async (
                int auditorUserId,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            (await dispatcher.SendAsync(
                new GetAuditorVerificationActivityQuery(auditorUserId), ct)).ToHttpResult())
            .RequireCapability(Capabilities.FieldAssets.Manage)
            .WithName("GetAuditorVerificationActivity")
            .Produces<GetAuditorVerificationActivityResponse>(StatusCodes.Status200OK);
    }
}

public sealed class SearchAuditorVerificationCountsHandler(VerificationDbContext db)
    : IRequestHandler<SearchAuditorVerificationCountsQuery, SearchAuditorVerificationCountsResponse>
{
    public async Task<Result<SearchAuditorVerificationCountsResponse>> HandleAsync(
        SearchAuditorVerificationCountsQuery request,
        CancellationToken ct)
    {
        var rows = await db.PhysicalVerifications.AsNoTracking()
            .GroupBy(item => item.VerifiedByUserId)
            .Select(group => new SearchAuditorVerificationCountsResponse.Row(
                group.Key,
                group.Select(item => item.AssetId).Distinct().Count()))
            .ToListAsync(ct);
        return new SearchAuditorVerificationCountsResponse(rows);
    }
}

public sealed class GetAuditorVerificationActivityHandler(
    VerificationDbContext db,
    IAssetSnapshot assets)
    : IRequestHandler<GetAuditorVerificationActivityQuery, GetAuditorVerificationActivityResponse>
{
    public async Task<Result<GetAuditorVerificationActivityResponse>> HandleAsync(
        GetAuditorVerificationActivityQuery request,
        CancellationToken ct)
    {
        if (request.AuditorUserId <= 0)
        {
            return Error.Validation("Auditor.InvalidId", "Select a valid auditor.");
        }

        var query = db.PhysicalVerifications.AsNoTracking()
            .Where(item => item.VerifiedByUserId == request.AuditorUserId);
        var total = await query.Select(item => item.AssetId).Distinct().CountAsync(ct);
        var verifications = await query
            .OrderByDescending(item => item.VerifiedOnUtc)
            .ThenByDescending(item => item.Id)
            .Take(100)
            .Select(item => new
            {
                item.Id,
                item.AssetId,
                AuditId = item.PhysicalVerificationCycleId,
                item.WorkingCondition,
                item.VerifiedOnUtc,
                item.Remarks,
            })
            .ToListAsync(ct);
        var auditIds = verifications.Select(item => item.AuditId).Distinct().ToArray();
        var auditNames = await db.PhysicalVerificationCycles.AsNoTracking()
            .Where(cycle => auditIds.Contains(cycle.Id))
            .ToDictionaryAsync(cycle => cycle.Id, cycle => cycle.CycleName, ct);
        var assetIds = verifications.Select(item => item.AssetId).Distinct().ToArray();
        var assetSnapshots = (await assets.GetManyAsync(assetIds, ct))
            .ToDictionary(asset => asset.AssetId);

        var rows = new List<GetAuditorVerificationActivityResponse.Row>(verifications.Count);
        foreach (var verification in verifications)
        {
            assetSnapshots.TryGetValue(verification.AssetId, out var asset);
            rows.Add(new GetAuditorVerificationActivityResponse.Row(
                verification.Id,
                verification.AssetId,
                asset?.AssetNumber ?? $"Asset {verification.AssetId}",
                asset?.AssetName ?? asset?.AssetNumber ?? $"Asset {verification.AssetId}",
                verification.AuditId,
                auditNames.GetValueOrDefault(verification.AuditId, $"Audit {verification.AuditId}"),
                verification.WorkingCondition,
                AsUtc(verification.VerifiedOnUtc),
                verification.Remarks));
        }

        return new GetAuditorVerificationActivityResponse(request.AuditorUserId, total, rows);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc
        ? value
        : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
