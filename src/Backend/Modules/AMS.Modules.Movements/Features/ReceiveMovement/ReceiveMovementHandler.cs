using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Movements.Domain;
using AMS.Modules.Movements.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Movements.Features.ReceiveMovement;

/// <summary>
/// Confirm arrival at the destination. Catalogue: Receive at the destination,
/// and Goods receipt at head office.
/// </summary>
/// <remarks>
/// <para>
/// This is the ONLY place an asset's branch changes. Everything before it
/// leaves the asset belonging to neither end, which is the truth while it is on
/// a lorry.
/// </para>
/// <para>
/// The batch closes when its last outstanding item is received. Counting the
/// remaining rows rather than decrementing a counter means a receipt that rolls
/// back cannot leave the count wrong.
/// </para>
/// </remarks>
public sealed class ReceiveMovementHandler(
    MovementsDbContext db,
    IAssetCustody custody,
    IAssetTimeline timeline,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<ReceiveMovementCommand, ReceiveMovementResponse>
{
    public async Task<Result<ReceiveMovementResponse>> HandleAsync(
        ReceiveMovementCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var movement = await db.AssetMovements.SingleOrDefaultAsync(m => m.Id == request.Id, ct);
        if (movement is null)
        {
            return Error.NotFound("Movement", request.Id);
        }

        if (movement.Status == MovementStatus.Received)
        {
            return Error.Conflict(
                "Movement.AlreadyReceived", "That shipment was already received.");
        }

        if (movement.Status == MovementStatus.Cancelled)
        {
            return Error.Conflict(
                "Movement.Cancelled", "That shipment was cancelled and cannot be received.");
        }

        // The asset moves through the contract, not through this module's
        // context: Asset.CurrentLocationId lives in [Assets] (01 rule 4a).
        var moved = await custody.ReceiveAtLocationAsync(
            movement.AssetId, movement.ToLocationId, ct);
        if (!moved)
        {
            return Error.NotFound("Asset", movement.AssetId);
        }

        // CK_AssetMovement_ReceiptPair ties these two together, so they are set
        // together or the row is refused.
        movement.Status = MovementStatus.Received;
        movement.ReceivedOnUtc = clock.UtcNow;
        movement.ReceivedByUserId = currentUser.Id;
        movement.ReceiptRemarks = request.ReceiptRemarks;
        movement.ModifiedOnUtc = clock.UtcNow;
        movement.ModifiedBy = currentUser.Username;

        await timeline.AppendAsync(
            new AssetTimelineEntry(
                movement.AssetId,
                "Received",
                request.ReceiptRemarks is null
                    ? $"Received at branch {movement.ToLocationId}."
                    : $"Received at branch {movement.ToLocationId}: {request.ReceiptRemarks}",
                clock.UtcNow,
                currentUser.Username,
                LocationId: movement.ToLocationId,
                MovementId: movement.Id),
            ct);

        var batchComplete = false;
        if (movement.MovementBatchId is { } batchId)
        {
            var stillOut = await db.AssetMovements.CountAsync(
                m => m.MovementBatchId == batchId
                     && m.Id != movement.Id
                     && m.Status == MovementStatus.InTransit,
                ct);

            if (stillOut == 0)
            {
                var batch = await db.MovementBatches.SingleAsync(b => b.Id == batchId, ct);
                batch.ReceivedOnUtc = clock.UtcNow;
                batch.ModifiedOnUtc = clock.UtcNow;
                batch.ModifiedBy = currentUser.Username;
                batchComplete = true;
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        return new ReceiveMovementResponse(
            movement.Id, movement.AssetId, movement.ToLocationId, batchComplete);
    }
}
