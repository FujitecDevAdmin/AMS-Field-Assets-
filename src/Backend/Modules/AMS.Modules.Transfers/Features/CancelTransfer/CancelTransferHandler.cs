using AMS.Modules.Transfers.Domain;
using AMS.Modules.Transfers.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Transfers.Features.CancelTransfer;

/// <summary>Withdraw a transfer before it is completed. Catalogue: Cancel.</summary>
/// <remarks>
/// A completed transfer cannot be cancelled. The change is already in the
/// register and possibly already in SAP; undoing it is a NEW transfer in the
/// other direction, which is the only version of the story that stays true.
/// </remarks>
public sealed class CancelTransferHandler(
    TransfersDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CancelTransferCommand, CancelTransferResponse>
{
    public async Task<Result<CancelTransferResponse>> HandleAsync(
        CancelTransferCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transfer = await db.AssetTransferRequests
            .SingleOrDefaultAsync(r => r.Id == request.Id, ct);
        if (transfer is null)
        {
            return Error.NotFound("Transfer", request.Id);
        }

        if (transfer.Status == TransferStatus.Completed)
        {
            return Error.Conflict(
                "Transfer.AlreadyCompleted",
                "That transfer has been applied. Raise one in the other direction instead.");
        }

        if (transfer.Status == TransferStatus.Cancelled)
        {
            return new CancelTransferResponse(transfer.Id, transfer.Status);
        }

        transfer.Status = TransferStatus.Cancelled;
        transfer.Remarks = request.Reason;
        transfer.ModifiedOnUtc = clock.UtcNow;
        transfer.ModifiedBy = currentUser.Username;

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

        return new CancelTransferResponse(transfer.Id, transfer.Status);
    }
}
