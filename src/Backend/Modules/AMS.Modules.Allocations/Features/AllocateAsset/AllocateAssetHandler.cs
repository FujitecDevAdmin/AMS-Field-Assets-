using AMS.Modules.Allocations.Domain;
using AMS.Modules.Allocations.Persistence;
using AMS.Modules.Assets.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.AllocateAsset;

/// <summary>
/// Assign an available asset to an employee. Catalogue: Allocate an asset.
/// </summary>
/// <remarks>
/// <para>
/// No read-then-write check that the asset is free. UX_AssetAllocation_
/// OneActivePerAsset is a filtered unique index over AssetId where
/// ReturnedOnUtc IS NULL, so a second live allocation collides on 2601 and
/// comes back as a 409. Checking first would be a race with a nicer error
/// message (03 rule 6).
/// </para>
/// <para>
/// The timeline entry goes through IAssetTimeline - Assets' contract, not its
/// tables - and both commit in the transaction the dispatcher owns (rule 4a).
/// </para>
/// </remarks>
public sealed class AllocateAssetHandler(
    AllocationsDbContext db,
    IAssetTimeline timeline,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<AllocateAssetCommand, AllocateAssetResponse>
{
    public async Task<Result<AllocateAssetResponse>> HandleAsync(
        AllocateAssetCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        AssetAllocationApproval? approval = null;
        if (request.ApprovalId is { } approvalId)
        {
            approval = await db.AssetAllocationApprovals
                .SingleOrDefaultAsync(a => a.Id == approvalId, ct);
            if (approval is null)
            {
                return Error.NotFound("AllocationRequest", approvalId);
            }

            if (approval.Status != ApprovalStatus.Approved)
            {
                return Error.Conflict(
                    "AllocationRequest.NotApproved",
                    "That request has not been approved, so it cannot be acted on.");
            }

            if (approval.AllocationId is not null)
            {
                return Error.Conflict(
                    "AllocationRequest.AlreadyActioned",
                    "That request has already produced an allocation.");
            }
        }

        var allocation = new AssetAllocation
        {
            AssetId = request.AssetId,
            EmployeeId = request.EmployeeId,
            LocationId = request.LocationId,
            AllocatedOnUtc = clock.UtcNow,
            ExpectedReturnDate = request.ExpectedReturnDate,
            Remarks = request.Remarks,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.AssetAllocations.Add(allocation);

        try
        {
            await db.SaveChangesAsync(ct);

            // Pending until the employee signs. The acknowledgement exists from
            // the start so "not signed yet" is a state rather than a missing row.
            db.AssetAcknowledgements.Add(new AssetAcknowledgement
            {
                AllocationId = allocation.Id,
                Status = AcknowledgementStatus.Pending,
                CreatedOnUtc = clock.UtcNow,
                CreatedBy = currentUser.Username,
            });

            if (approval is not null)
            {
                approval.AllocationId = allocation.Id;
                approval.ModifiedOnUtc = clock.UtcNow;
                approval.ModifiedBy = currentUser.Username;
            }

            await timeline.AppendAsync(
                new AssetTimelineEntry(
                    request.AssetId,
                    "Allocated",
                    $"Issued to employee {request.EmployeeId}.",
                    clock.UtcNow,
                    currentUser.Username,
                    EmployeeId: request.EmployeeId,
                    LocationId: request.LocationId,
                    AllocationId: allocation.Id),
                ct);

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

        return new AllocateAssetResponse(allocation.Id, allocation.AssetId, allocation.EmployeeId);
    }
}
