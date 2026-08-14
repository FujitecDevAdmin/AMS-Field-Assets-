using AMS.Modules.Allocations.Domain;
using AMS.Modules.Allocations.Persistence;
using AMS.Modules.Assets.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.ReverseReturn;

/// <summary>
/// Restore an allocation closed in error. Catalogue: Reverse a return made in
/// error - records who reversed it and why.
/// </summary>
/// <remarks>
/// <para>
/// The reversal is a ROW, not an undo. Clearing ReturnedOnUtc and leaving no
/// trace would make the register disagree with everybody's memory of what
/// happened, and the person who made the mistake is not the person who later
/// has to explain it.
/// </para>
/// <para>
/// It can fail on UX_AssetAllocation_OneActivePerAsset, and that is correct: if
/// the asset was issued to somebody else after the return, putting this
/// allocation back would mean two people holding one asset. The 409 says so.
/// </para>
/// </remarks>
public sealed class ReverseReturnHandler(
    AllocationsDbContext db,
    IAssetTimeline timeline,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<ReverseReturnCommand, ReverseReturnResponse>
{
    public async Task<Result<ReverseReturnResponse>> HandleAsync(
        ReverseReturnCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var allocation = await db.AssetAllocations.SingleOrDefaultAsync(a => a.Id == request.Id, ct);
        if (allocation is null)
        {
            return Error.NotFound("Allocation", request.Id);
        }

        if (allocation.ReturnedOnUtc is not { } previousReturn)
        {
            return Error.Conflict(
                "Allocation.NotReturned", "That allocation is still open, so there is nothing to reverse.");
        }

        // A handover means the asset physically left the employee. Reversing the
        // paperwork would say somebody holds a thing that is sitting in a store.
        var handover = await db.AssetHandovers
            .Where(h => h.AllocationId == allocation.Id && h.CancelledOnUtc == null)
            .FirstOrDefaultAsync(ct);
        if (handover is not null && handover.Status != HandoverStatus.Cancelled)
        {
            return Error.Conflict(
                "Allocation.HandedOver",
                "That asset was handed into a branch store. Cancel the handover first.");
        }

        var reversal = new AllocationReturnReversal
        {
            AllocationId = allocation.Id,
            HandoverId = handover?.Id,
            Reason = request.Reason,
            PreviousReturnedOnUtc = previousReturn,
            RestoredEmployeeId = allocation.EmployeeId,
            ReversedByUserId = currentUser.Id,
            ReversedOnUtc = clock.UtcNow,
        };
        db.AllocationReturnReversals.Add(reversal);

        allocation.ReturnedOnUtc = null;
        allocation.ReceivedByUserId = null;
        allocation.ModifiedOnUtc = clock.UtcNow;
        allocation.ModifiedBy = currentUser.Username;

        await timeline.AppendAsync(
            new AssetTimelineEntry(
                allocation.AssetId,
                "ReturnReversed",
                $"Return reversed: {request.Reason}",
                clock.UtcNow,
                currentUser.Username,
                EmployeeId: allocation.EmployeeId,
                LocationId: allocation.LocationId,
                AllocationId: allocation.Id),
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

        return new ReverseReturnResponse(reversal.Id, allocation.Id);
    }
}
