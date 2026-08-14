using AMS.Modules.Allocations.Domain;
using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.ApproveAcknowledgement;

/// <summary>The manager countersigns. Catalogue: Approve the acknowledgement.</summary>
/// <remarks>
/// The employee cannot countersign their own. The countersignature exists to be
/// a second person's word, and one signature entered twice is not that.
/// </remarks>
public sealed class ApproveAcknowledgementHandler(
    AllocationsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<ApproveAcknowledgementCommand, ApproveAcknowledgementResponse>
{
    public async Task<Result<ApproveAcknowledgementResponse>> HandleAsync(
        ApproveAcknowledgementCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var allocation = await db.AssetAllocations
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == request.AllocationId, ct);
        if (allocation is null)
        {
            return Error.NotFound("Allocation", request.AllocationId);
        }

        var acknowledgement = await db.AssetAcknowledgements
            .SingleOrDefaultAsync(k => k.AllocationId == request.AllocationId, ct);
        if (acknowledgement is null)
        {
            return Error.NotFound("Acknowledgement", request.AllocationId);
        }

        if (currentUser.EmployeeId == allocation.EmployeeId)
        {
            return Error.Validation(
                "Acknowledgement.SelfApproval",
                "An employee cannot countersign their own acknowledgement.");
        }

        if (acknowledgement.Status == AcknowledgementStatus.Pending)
        {
            return Error.Conflict(
                "Acknowledgement.NotSigned",
                "The employee has not signed yet, so there is nothing to countersign.");
        }

        if (acknowledgement.Status == AcknowledgementStatus.Approved)
        {
            return Error.Conflict(
                "Acknowledgement.AlreadyApproved", "That acknowledgement was already countersigned.");
        }

        acknowledgement.Status = AcknowledgementStatus.Approved;
        acknowledgement.ManagerUserId = currentUser.Id;
        acknowledgement.ManagerApprovedOnUtc = clock.UtcNow;
        acknowledgement.ModifiedOnUtc = clock.UtcNow;
        acknowledgement.ModifiedBy = currentUser.Username;

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

        return new ApproveAcknowledgementResponse(acknowledgement.Id, acknowledgement.Status);
    }
}
