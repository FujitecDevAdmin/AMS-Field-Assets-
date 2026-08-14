using System.Globalization;
using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Movements.Domain;
using AMS.Modules.Movements.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AMS.Modules.Movements.Features.DespatchBatch;

/// <summary>
/// Send several assets on one consignment. Catalogue: Despatch several assets
/// at once - one invoice and courier, every asset gets its own tracking row.
/// </summary>
/// <remarks>
/// The invoice and courier live once on the batch and not on each asset. The
/// design script's note is blunt about why: three rows carrying one invoice
/// number is three chances for somebody to edit the third one.
/// </remarks>
public sealed class DespatchBatchHandler(
    MovementsDbContext db,
    IAssetTimeline timeline,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<DespatchBatchCommand, DespatchBatchResponse>
{
    public async Task<Result<DespatchBatchResponse>> HandleAsync(
        DespatchBatchCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MovementType.All.Contains(request.MovementType, StringComparer.Ordinal))
        {
            return Error.Validation(
                "Movement.UnknownType",
                $"Movement type must be one of {string.Join(", ", MovementType.All)}.");
        }

        var assetIds = request.AssetIds.Distinct().ToList();
        if (assetIds.Count == 0)
        {
            // CK_MovementBatch_PositiveCount would catch it too, as a 500. This
            // says it in words beside the empty selection.
            return Error.Validation(
                "Movement.EmptyBatch", "Select at least one asset to despatch.");
        }

        var alreadyMoving = await db.AssetMovements
            .Where(m => assetIds.Contains(m.AssetId) && m.Status == MovementStatus.InTransit)
            .Select(m => m.AssetId)
            .ToListAsync(ct);
        if (alreadyMoving.Count > 0)
        {
            return Error.Conflict(
                "Movement.AlreadyInTransit",
                $"{alreadyMoving.Count} of the selected assets are already in transit.");
        }

        var batch = new MovementBatch
        {
            BatchNumber = await NextBatchNumberAsync(ct),
            FromLocationId = request.FromLocationId,
            ToLocationId = request.ToLocationId,
            MovementType = request.MovementType,
            InvoiceNumber = request.InvoiceNumber,
            InvoiceDate = request.InvoiceDate,
            CourierName = request.CourierName,
            TrackingNumber = request.TrackingNumber,
            ChallanNumber = request.ChallanNumber,
            Remarks = request.Remarks,
            ItemCount = assetIds.Count,
            DispatchedByUserId = currentUser.Id,
            ShippedOnUtc = clock.UtcNow,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };
        db.MovementBatches.Add(batch);

        try
        {
            await db.SaveChangesAsync(ct);

            foreach (var assetId in assetIds)
            {
                db.AssetMovements.Add(new Domain.AssetMovement
                {
                    AssetId = assetId,
                    MovementBatchId = batch.Id,
                    MovementType = request.MovementType,
                    FromLocationId = request.FromLocationId,
                    ToLocationId = request.ToLocationId,
                    Quantity = 1m,
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
                });

                await timeline.AppendAsync(
                    new AssetTimelineEntry(
                        assetId,
                        "Despatched",
                        $"Despatched on consignment {batch.BatchNumber} to branch {request.ToLocationId}.",
                        clock.UtcNow,
                        currentUser.Username,
                        LocationId: request.FromLocationId),
                    ct);
            }

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

        return new DespatchBatchResponse(batch.Id, batch.BatchNumber, batch.ItemCount);
    }

    /// <summary>
    /// The next consignment number, from the database sequence.
    /// </summary>
    /// <remarks>
    /// A sequence and not MAX+1: two branches despatching at the same moment
    /// would both read the same maximum, and UX_MovementBatch_Number would then
    /// reject one of them for no reason a user could act on.
    /// </remarks>
    private async Task<string> NextBatchNumberAsync(CancellationToken ct)
    {
        // A direct command, not SqlQuery<T>: EF wraps that in a subquery and
        // NEXT VALUE FOR is illegal inside one. Issued on the context's own
        // connection and transaction so the number is drawn inside the command's
        // transaction like everything else - a sequence does not roll back, but
        // the batch that used it must.
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT NEXT VALUE FOR [Movements].[MovementBatchNumberSequence];";
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();

        var next = Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);

        return $"MB-{next:0000000}";
    }
}
