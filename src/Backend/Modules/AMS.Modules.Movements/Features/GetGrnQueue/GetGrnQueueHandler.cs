using AMS.Modules.Movements.Domain;
using AMS.Modules.Movements.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Movements.Features.GetGrnQueue;

/// <summary>
/// Pending receipts at the destination. Catalogue screen: GRN Queue.
/// </summary>
/// <remarks>
/// Oldest first, deliberately. This queue exists to be worked, and something
/// despatched three weeks ago that never arrived is the row somebody needs to
/// chase — sorting newest first would bury it under this morning's parcels.
/// </remarks>
public sealed class GetGrnQueueHandler(
    MovementsDbContext db,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<GetGrnQueueQuery, GetGrnQueueResponse>
{
    public async Task<Result<GetGrnQueueResponse>> HandleAsync(
        GetGrnQueueQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = clock.UtcNow;

        var query = db.AssetMovements
            .AsNoTracking()
            .Where(m => m.Status == MovementStatus.InTransit);

        // The RECEIVING branch only. A queue of things arriving somewhere else
        // is not a queue anybody here can act on.
        if (!currentUser.HasAllBranches)
        {
            var branches = currentUser.BranchIds;
            query = query.Where(m => branches.Contains(m.ToLocationId));
        }

        if (request.ToLocationId.HasValue)
        {
            query = query.Where(m => m.ToLocationId == request.ToLocationId.Value);
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(m => m.ShippedOnUtc)
            .ThenBy(m => m.Id)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(m => new GetGrnQueueResponse.Row(
                m.Id,
                m.AssetId,
                m.MovementBatchId,
                db.MovementBatches.Where(b => b.Id == m.MovementBatchId)
                    .Select(b => b.BatchNumber).FirstOrDefault(),
                m.FromLocationId,
                m.ToLocationId,
                m.Quantity,
                m.CourierName,
                m.TrackingNumber,
                m.ChallanNumber,
                m.ShippedOnUtc,
                EF.Functions.DateDiffDay(m.ShippedOnUtc, now)))
            .ToListAsync(ct);

        return new GetGrnQueueResponse(rows, total);
    }
}
