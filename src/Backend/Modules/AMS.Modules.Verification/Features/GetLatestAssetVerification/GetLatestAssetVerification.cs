using AMS.Modules.Identity.PublicApi.Identity;
using AMS.Modules.Verification.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Features.GetLatestAssetVerification;

public sealed record GetLatestAssetVerificationQuery(int AssetId)
    : IQuery<GetLatestAssetVerificationResponse>;

public sealed record GetLatestAssetVerificationResponse(
    bool IsVerified,
    int? VerificationId,
    int? AuditId,
    int? VerifiedByUserId,
    string? AuditorName,
    DateTime? VerifiedOnUtc,
    string? Remarks);

public static class GetLatestAssetVerificationEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/assets/{assetId:int}/latest-verification", async (
                int assetId,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var result = await dispatcher.SendAsync(
                    new GetLatestAssetVerificationQuery(assetId), ct);
                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.FieldAssets.Manage)
            .WithName("GetLatestAssetVerification")
            .Produces<GetLatestAssetVerificationResponse>(StatusCodes.Status200OK);
    }
}

public sealed class GetLatestAssetVerificationHandler(
    VerificationDbContext db,
    IUserDirectory users)
    : IRequestHandler<GetLatestAssetVerificationQuery, GetLatestAssetVerificationResponse>
{
    public async Task<Result<GetLatestAssetVerificationResponse>> HandleAsync(
        GetLatestAssetVerificationQuery request,
        CancellationToken ct)
    {
        if (request.AssetId <= 0)
        {
            return Error.Validation("Asset.InvalidId", "Select a valid asset.");
        }

        var verification = await db.PhysicalVerifications.AsNoTracking()
            .Where(item => item.AssetId == request.AssetId)
            .OrderByDescending(item => item.VerifiedOnUtc)
            .ThenByDescending(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.PhysicalVerificationCycleId,
                item.VerifiedByUserId,
                item.VerifiedOnUtc,
                item.Remarks,
                item.CreatedBy,
            })
            .FirstOrDefaultAsync(ct);

        if (verification is null)
        {
            return new GetLatestAssetVerificationResponse(
                false, null, null, null, null, null, null);
        }

        var auditor = await users.FindAsync(verification.VerifiedByUserId, ct);
        return new GetLatestAssetVerificationResponse(
            true,
            verification.Id,
            verification.PhysicalVerificationCycleId,
            verification.VerifiedByUserId,
            auditor?.DisplayName ?? verification.CreatedBy ?? $"User #{verification.VerifiedByUserId}",
            AsUtc(verification.VerifiedOnUtc),
            verification.Remarks);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc
        ? value
        : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
