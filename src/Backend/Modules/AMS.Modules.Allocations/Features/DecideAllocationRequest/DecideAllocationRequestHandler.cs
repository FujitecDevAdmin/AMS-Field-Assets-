using AMS.Modules.Allocations.Domain;
using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.DecideAllocationRequest;

/// <summary>
/// Approve or reject a request. Catalogue: with a decision remark that stays on
/// the record.
/// </summary>
/// <remarks>
/// Deciding does not allocate. Approval says "yes, when you can" - the asset
/// may still be with somebody else, and the allocation is a separate act by a
/// separate capability. Collapsing the two would let an approver issue assets.
/// </remarks>
public sealed class DecideAllocationRequestHandler(
    AllocationsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<DecideAllocationRequestCommand, DecideAllocationRequestResponse>
{
    public async Task<Result<DecideAllocationRequestResponse>> HandleAsync(
        DecideAllocationRequestCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var approval = await db.AssetAllocationApprovals
            .SingleOrDefaultAsync(a => a.Id == request.Id, ct);
        if (approval is null)
        {
            return Error.NotFound("AllocationRequest", request.Id);
        }

        // Deciding twice is not idempotent - it would overwrite the first
        // decision and the remark explaining it.
        if (approval.Status != ApprovalStatus.Pending)
        {
            return Error.Conflict(
                "AllocationRequest.AlreadyDecided",
                $"That request was already {approval.Status.ToLowerInvariant()}.");
        }

        approval.Status = request.Approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        approval.DecidedByUserId = currentUser.Id;
        approval.DecidedOnUtc = clock.UtcNow;
        approval.DecisionRemarks = request.DecisionRemarks;
        approval.ModifiedOnUtc = clock.UtcNow;
        approval.ModifiedBy = currentUser.Username;

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

        return new DecideAllocationRequestResponse(approval.Id, approval.Status);
    }
}
