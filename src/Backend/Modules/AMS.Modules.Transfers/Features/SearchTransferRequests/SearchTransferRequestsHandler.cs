using AMS.Modules.Transfers.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Transfers.Features.SearchTransferRequests;

/// <summary>
/// The transfer queue and its SAP status. Catalogue screen: Transfer Requests.
/// </summary>
public sealed class SearchTransferRequestsHandler(
    TransfersDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<SearchTransferRequestsQuery, SearchTransferRequestsResponse>
{
    public async Task<Result<SearchTransferRequestsResponse>> HandleAsync(
        SearchTransferRequestsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.AssetTransferRequests.AsNoTracking();

        // Both ends again: a branch cares about what is leaving it AND what is
        // arriving, and a transfer with neither end set - a cost-centre move -
        // belongs to whoever can see the asset, so it stays visible.
        if (!currentUser.HasAllBranches)
        {
            var branches = currentUser.BranchIds;
            query = query.Where(r =>
                (r.FromLocationId == null && r.ToLocationId == null)
                || (r.FromLocationId != null && branches.Contains(r.FromLocationId.Value))
                || (r.ToLocationId != null && branches.Contains(r.ToLocationId.Value)));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(r => r.Status == request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.TransferType))
        {
            query = query.Where(r => r.TransferType == request.TransferType);
        }

        if (request.AssetId.HasValue)
        {
            query = query.Where(r => r.AssetId == request.AssetId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SapSyncStatus))
        {
            query = query.Where(r => r.SapSyncStatus == request.SapSyncStatus);
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(r => r.RequestedOnUtc)
            .ThenByDescending(r => r.Id)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(r => new SearchTransferRequestsResponse.Row(
                r.Id, r.AssetId, r.TransferType, r.Status,
                r.FromEmployeeId, r.ToEmployeeId,
                r.FromDepartmentId, r.ToDepartmentId,
                r.FromLocationId, r.ToLocationId,
                r.FromCostCenter, r.ToCostCenter,
                r.RequestedByUserId, r.RequestedOnUtc,
                r.ApprovedByUserId, r.ApprovedOnUtc, r.CompletedOnUtc,
                r.Remarks, r.MovementId, r.SapSyncStatus))
            .ToListAsync(ct);

        return new SearchTransferRequestsResponse(rows, total);
    }
}
