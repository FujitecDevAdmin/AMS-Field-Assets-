using AMS.Modules.Allocations.Domain;
using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.RequestAllocation;

/// <summary>
/// Ask for an asset to be allocated. Catalogue: Request an asset for an
/// employee - raises an approval request rather than allocating directly.
/// </summary>
/// <remarks>
/// Deliberately does not check that the asset is free. It might be returned by
/// the time somebody decides, and refusing here would make the queue a race
/// against other people's returns. The check that matters happens at
/// allocation, where the filtered unique index enforces it.
/// </remarks>
public sealed class RequestAllocationHandler(
    AllocationsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<RequestAllocationCommand, RequestAllocationResponse>
{
    public async Task<Result<RequestAllocationResponse>> HandleAsync(
        RequestAllocationCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // One open request per asset per employee. Two identical requests in
        // the queue means two people decide the same thing differently.
        var duplicate = await db.AssetAllocationApprovals.AnyAsync(
            a => a.AssetId == request.AssetId
                 && a.EmployeeId == request.EmployeeId
                 && a.Status == ApprovalStatus.Pending,
            ct);
        if (duplicate)
        {
            return Error.Conflict(
                "AllocationRequest.AlreadyPending",
                "There is already a pending request for that asset and employee.");
        }

        var approval = new AssetAllocationApproval
        {
            AssetId = request.AssetId,
            EmployeeId = request.EmployeeId,
            LocationId = request.LocationId,
            Status = ApprovalStatus.Pending,
            RequestedByUserId = currentUser.Id,
            RequestedOnUtc = clock.UtcNow,
            DecisionRemarks = request.Remarks,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.AssetAllocationApprovals.Add(approval);

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

        return new RequestAllocationResponse(approval.Id, approval.Status);
    }
}
