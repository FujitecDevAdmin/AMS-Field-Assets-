using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Movements.Domain;
using AMS.Modules.Movements.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Movements.Features.DespatchAsset;

/// <summary>
/// Send one asset to another branch or to head office. Catalogue: Despatch an
/// asset, with courier, tracking and challan.
/// </summary>
/// <remarks>
/// <b>Does not touch the asset's branch.</b> An asset in transit belongs to
/// neither end, and the design script says why: marking it as arrived on
/// despatch makes it findable somewhere it is not. The move happens once, on
/// receipt, through IAssetCustody.
/// </remarks>
public sealed class DespatchAssetHandler(
    MovementsDbContext db,
    IAssetTimeline timeline,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<DespatchAssetCommand, DespatchAssetResponse>
{
    public async Task<Result<DespatchAssetResponse>> HandleAsync(
        DespatchAssetCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MovementType.All.Contains(request.MovementType, StringComparer.Ordinal))
        {
            return Error.Validation(
                "Movement.UnknownType",
                $"Movement type must be one of {string.Join(", ", MovementType.All)}.");
        }

        // One in-flight shipment per asset. Two would mean the same thing is on
        // two lorries, and whichever receipt lands second would move an asset
        // that is already somewhere else.
        var alreadyMoving = await db.AssetMovements.AnyAsync(
            m => m.AssetId == request.AssetId && m.Status == MovementStatus.InTransit, ct);
        if (alreadyMoving)
        {
            return Error.Conflict(
                "Movement.AlreadyInTransit", "That asset is already in transit.");
        }

        var movement = new Domain.AssetMovement
        {
            AssetId = request.AssetId,
            HandoverId = request.HandoverId,
            MovementType = request.MovementType,
            FromLocationId = request.FromLocationId,
            ToLocationId = request.ToLocationId,
            Quantity = request.Quantity,
            Status = MovementStatus.InTransit,
            CourierName = request.CourierName,
            TrackingNumber = request.TrackingNumber,
            ChallanNumber = request.ChallanNumber,
            InvoiceNumber = request.InvoiceNumber,
            InvoiceDate = request.InvoiceDate,
            Remarks = request.Remarks,
            ShippedOnUtc = clock.UtcNow,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.AssetMovements.Add(movement);

        await timeline.AppendAsync(
            new AssetTimelineEntry(
                request.AssetId,
                "Despatched",
                $"Despatched from branch {request.FromLocationId} to branch {request.ToLocationId}.",
                clock.UtcNow,
                currentUser.Username,
                LocationId: request.FromLocationId),
            ct);

        try
        {
            await db.SaveChangesAsync(ct);

            // The timeline entry needs the movement id, which only exists after
            // the save. Appending a second time would duplicate the line, so the
            // link is set here instead.
            movement.ModifiedOnUtc = clock.UtcNow;
            movement.ModifiedBy = currentUser.Username;
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

        return new DespatchAssetResponse(movement.Id, movement.AssetId, movement.Status);
    }
}
