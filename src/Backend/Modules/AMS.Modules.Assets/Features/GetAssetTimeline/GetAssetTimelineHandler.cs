using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.GetAssetTimeline;

/// <summary>Everything that has happened to one asset, newest first.</summary>
/// <remarks>
/// Paged. An asset that has been allocated, returned, moved, ticketed and
/// verified for five years accumulates hundreds of lines, and the screen only
/// ever shows the top of them.
///
/// Ordered by <c>EventOnUtc</c> descending and then by id descending. The id
/// tiebreak is not decoration: several modules append in one transaction — a
/// movement receipt writes its own line and a status change alongside it — so
/// entries genuinely share a timestamp, and without it the two would swap
/// places between page loads.
/// </remarks>
public sealed class GetAssetTimelineHandler(AssetsDbContext db)
    : IRequestHandler<GetAssetTimelineQuery, GetAssetTimelineResponse>
{
    public async Task<Result<GetAssetTimelineResponse>> HandleAsync(
        GetAssetTimelineQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await db.Assets.AnyAsync(a => a.Id == request.AssetId, ct))
        {
            return Error.NotFound("Asset", request.AssetId);
        }

        var query = db.AssetEvents.AsNoTracking().Where(e => e.AssetId == request.AssetId);

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(e => e.EventOnUtc)
            .ThenByDescending(e => e.Id)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(e => new GetAssetTimelineResponse.Row(
                e.Id,
                e.EventType,
                e.Description,
                e.EventOnUtc,
                e.PerformedBy,
                e.EmployeeId,
                e.EmployeeNameSnapshot,
                e.LocationId,
                e.LocationNameSnapshot,
                e.QuantityDelta,
                e.AllocationId,
                e.MovementId,
                e.ServiceRequestId,
                e.ContractId,
                e.HandoverId,
                e.VerificationId,
                e.DisposalId))
            .ToListAsync(ct);

        return new GetAssetTimelineResponse(rows, total);
    }
}
