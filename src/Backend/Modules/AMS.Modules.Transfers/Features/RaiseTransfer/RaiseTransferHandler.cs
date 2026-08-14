using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Transfers.Domain;
using AMS.Modules.Transfers.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Transfers.Features.RaiseTransfer;

/// <summary>
/// Raise a transfer. Catalogue: by employee, department, branch or cost centre.
/// </summary>
/// <remarks>
/// The "from" side is captured from the asset AS IT IS NOW, not supplied by the
/// caller. A form that let somebody type where an asset came from is a form
/// that lets them record a move that never happened.
/// </remarks>
public sealed class RaiseTransferHandler(
    TransfersDbContext db,
    IAssetSnapshot assets,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<RaiseTransferCommand, RaiseTransferResponse>
{
    public async Task<Result<RaiseTransferResponse>> HandleAsync(
        RaiseTransferCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TransferType.All.Contains(request.TransferType, StringComparer.Ordinal))
        {
            return Error.Validation(
                "Transfer.UnknownType",
                $"Transfer type must be one of {string.Join(", ", TransferType.All)}.");
        }

        // CK_AssetTransferRequest_TypePair says this too, and would return a
        // 500. Saying it here names the field that is missing.
        var destinationMissing = request.TransferType switch
        {
            TransferType.Employee => request.ToEmployeeId is null,
            TransferType.Department => request.ToDepartmentId is null,
            TransferType.Branch => request.ToLocationId is null,
            _ => string.IsNullOrWhiteSpace(request.ToCostCenter),
        };
        if (destinationMissing)
        {
            return Error.Validation(
                "Transfer.DestinationRequired",
                $"A {request.TransferType} transfer needs a destination.");
        }

        var snapshot = await assets.GetAsync(request.AssetId, ct);
        if (snapshot is null)
        {
            return Error.NotFound("Asset", request.AssetId);
        }

        // One open transfer per asset. Two would apply in whatever order they
        // were completed, and the second would overwrite the first with values
        // captured before it happened.
        var alreadyOpen = await db.AssetTransferRequests.AnyAsync(
            r => r.AssetId == request.AssetId
                 && (r.Status == TransferStatus.Pending || r.Status == TransferStatus.Approved),
            ct);
        if (alreadyOpen)
        {
            return Error.Conflict(
                "Transfer.AlreadyOpen", "That asset already has a transfer awaiting completion.");
        }

        var transfer = new Domain.AssetTransferRequest
        {
            AssetId = request.AssetId,
            TransferType = request.TransferType,
            Status = TransferStatus.Pending,
            FromEmployeeId = snapshot.CurrentEmployeeId,
            ToEmployeeId = request.ToEmployeeId,
            FromDepartmentId = snapshot.DepartmentId,
            ToDepartmentId = request.ToDepartmentId,
            FromLocationId = snapshot.CurrentLocationId,
            ToLocationId = request.ToLocationId,
            FromCostCenter = snapshot.CostCenter,
            ToCostCenter = request.ToCostCenter,
            RequestedByUserId = currentUser.Id,
            RequestedOnUtc = clock.UtcNow,
            Remarks = request.Remarks,
            // Nothing is owed to SAP until the change is actually applied.
            SapSyncStatus = Domain.SapSyncStatus.NotRequired,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.AssetTransferRequests.Add(transfer);

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

        return new RaiseTransferResponse(transfer.Id, transfer.TransferType, transfer.Status);
    }
}
