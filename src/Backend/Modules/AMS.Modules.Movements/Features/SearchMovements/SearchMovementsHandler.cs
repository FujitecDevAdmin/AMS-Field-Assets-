using AMS.Modules.Movements.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Movements.Features.SearchMovements;

/// <summary>Shipments and where they have got to. Catalogue screen: Despatch.</summary>
/// <remarks>
/// Branch scoping matches BOTH ends. A shipment concerns the branch that sent
/// it and the branch expecting it, and showing it to only one of them would
/// leave the other unable to see what is coming.
/// </remarks>
public sealed class SearchMovementsHandler(MovementsDbContext db, ICurrentUser currentUser)
    : IRequestHandler<SearchMovementsQuery, SearchMovementsResponse>
{
    public async Task<Result<SearchMovementsResponse>> HandleAsync(
        SearchMovementsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.AssetMovements.AsNoTracking();

        if (!currentUser.HasAllBranches)
        {
            var branches = currentUser.BranchIds;
            query = query.Where(m => branches.Contains(m.FromLocationId)
                                  || branches.Contains(m.ToLocationId));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(m => m.Status == request.Status);
        }

        if (request.AssetId.HasValue)
        {
            query = query.Where(m => m.AssetId == request.AssetId.Value);
        }

        if (request.FromLocationId.HasValue)
        {
            query = query.Where(m => m.FromLocationId == request.FromLocationId.Value);
        }

        if (request.ToLocationId.HasValue)
        {
            query = query.Where(m => m.ToLocationId == request.ToLocationId.Value);
        }

        if (request.MovementBatchId.HasValue)
        {
            query = query.Where(m => m.MovementBatchId == request.MovementBatchId.Value);
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(m => m.ShippedOnUtc)
            .ThenByDescending(m => m.Id)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(m => new SearchMovementsResponse.Row(
                m.Id, m.AssetId, m.MovementBatchId, m.MovementType,
                m.FromLocationId, m.ToLocationId, m.Status, m.Quantity,
                m.CourierName, m.TrackingNumber, m.ChallanNumber,
                m.ShippedOnUtc, m.ReceivedOnUtc, m.ReceiptRemarks))
            .ToListAsync(ct);

        return new SearchMovementsResponse(rows, total);
    }
}
