using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Transfers.Domain;
using AMS.Modules.Transfers.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Transfers.Features.CompleteTransfer;

/// <summary>
/// Apply an approved transfer. Catalogue: applies the change and queues it to
/// SAP where the accounting system needs to know.
/// </summary>
/// <remarks>
/// <para>
/// Only the column the transfer is ABOUT is changed. A cost-centre transfer
/// that also restated who holds the asset would silently undo an allocation
/// made while it sat in the queue.
/// </para>
/// <para>
/// SAP is told about branch and cost-centre moves and not about employee or
/// department ones: those are AMS's own bookkeeping, and sending them would
/// queue thousands of rows the accounting system discards.
/// </para>
/// </remarks>
public sealed class CompleteTransferHandler(
    TransfersDbContext db,
    IAssetCustody custody,
    IAssetTimeline timeline,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CompleteTransferCommand, CompleteTransferResponse>
{
    public async Task<Result<CompleteTransferResponse>> HandleAsync(
        CompleteTransferCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transfer = await db.AssetTransferRequests
            .SingleOrDefaultAsync(r => r.Id == request.Id, ct);
        if (transfer is null)
        {
            return Error.NotFound("Transfer", request.Id);
        }

        if (transfer.Status != TransferStatus.Approved)
        {
            return Error.Conflict(
                "Transfer.NotApproved",
                transfer.Status == TransferStatus.Completed
                    ? "That transfer has already been completed."
                    : "Only an approved transfer can be completed.");
        }

        var applied = await custody.ApplyTransferAsync(
            transfer.AssetId,
            transfer.TransferType == TransferType.Employee ? transfer.ToEmployeeId : null,
            transfer.TransferType == TransferType.Department ? transfer.ToDepartmentId : null,
            transfer.TransferType == TransferType.Branch ? transfer.ToLocationId : null,
            transfer.TransferType == TransferType.CostCenter ? transfer.ToCostCenter : null,
            ct);
        if (!applied)
        {
            return Error.NotFound("Asset", transfer.AssetId);
        }

        transfer.Status = TransferStatus.Completed;
        transfer.CompletedOnUtc = clock.UtcNow;
        transfer.MovementId = request.MovementId;
        transfer.SapSyncStatus = NeedsSap(transfer.TransferType)
            ? Domain.SapSyncStatus.Pending
            : Domain.SapSyncStatus.NotRequired;
        transfer.ModifiedOnUtc = clock.UtcNow;
        transfer.ModifiedBy = currentUser.Username;

        await timeline.AppendAsync(
            new AssetTimelineEntry(
                transfer.AssetId,
                "Transferred",
                $"{transfer.TransferType} transfer completed.",
                clock.UtcNow,
                currentUser.Username,
                EmployeeId: transfer.ToEmployeeId,
                LocationId: transfer.ToLocationId ?? transfer.FromLocationId,
                MovementId: request.MovementId),
            ct);

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

        return new CompleteTransferResponse(
            transfer.Id, transfer.Status, transfer.SapSyncStatus);
    }

    /// <summary>
    /// Whether the accounting system needs telling.
    /// </summary>
    /// <remarks>
    /// Branch and cost centre change where the cost sits, which SAP owns.
    /// Employee and department are AMS's own record of who is responsible, and
    /// queueing those would put thousands of rows in front of a system that
    /// discards them.
    /// </remarks>
    private static bool NeedsSap(string transferType) =>
        transferType is TransferType.Branch or TransferType.CostCenter;
}
