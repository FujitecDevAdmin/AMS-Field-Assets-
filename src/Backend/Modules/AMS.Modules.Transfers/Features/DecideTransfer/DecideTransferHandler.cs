using AMS.Modules.Transfers.Domain;
using AMS.Modules.Transfers.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Transfers.Features.DecideTransfer;

/// <summary>Approve or reject a transfer. Catalogue: with a remark.</summary>
/// <remarks>
/// Approving does not apply anything. The asset may still need to travel, and
/// completing is a separate act by a separate capability — which is what stops
/// the person who wants a transfer from being the one who makes it true.
/// </remarks>
public sealed class DecideTransferHandler(
    TransfersDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<DecideTransferCommand, DecideTransferResponse>
{
    public async Task<Result<DecideTransferResponse>> HandleAsync(
        DecideTransferCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transfer = await db.AssetTransferRequests
            .SingleOrDefaultAsync(r => r.Id == request.Id, ct);
        if (transfer is null)
        {
            return Error.NotFound("Transfer", request.Id);
        }

        if (transfer.Status != TransferStatus.Pending)
        {
            return Error.Conflict(
                "Transfer.AlreadyDecided",
                $"That transfer is already {transfer.Status.ToLowerInvariant()}.");
        }

        transfer.Status = request.Approved ? TransferStatus.Approved : TransferStatus.Rejected;
        transfer.ApprovedByUserId = currentUser.Id;
        transfer.ApprovedOnUtc = clock.UtcNow;
        transfer.ModifiedOnUtc = clock.UtcNow;
        transfer.ModifiedBy = currentUser.Username;

        if (!string.IsNullOrWhiteSpace(request.Remarks))
        {
            transfer.Remarks = request.Remarks;
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

        return new DecideTransferResponse(transfer.Id, transfer.Status);
    }
}
