using AMS.Modules.Allocations.Domain;
using AMS.Modules.Allocations.Persistence;
using AMS.Modules.Assets.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.RecordHandover;

/// <summary>
/// Take an asset into the branch store. Catalogue: Hand an asset into the
/// branch store - records the condition and a mandatory remark, closes the
/// allocation.
/// </summary>
/// <remarks>
/// This is the stage the handbook has and AMS did not: employee to branch store
/// to transit to head office. Closing the allocation here is what frees the
/// asset; the despatch and GRN screens select from what this leaves behind.
/// </remarks>
public sealed class RecordHandoverHandler(
    AllocationsDbContext db,
    IAssetTimeline timeline,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<RecordHandoverCommand, RecordHandoverResponse>
{
    /// <summary>
    /// The handbook's limit, enforced here rather than in the database.
    /// </summary>
    /// <remarks>
    /// Section 18 of the design lists this among the rules the application owns:
    /// a CHECK cannot count rows in another table without a trigger, and a
    /// trigger is worse than this.
    /// </remarks>
    private const int MaxImages = 5;

    public async Task<Result<RecordHandoverResponse>> HandleAsync(
        RecordHandoverCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!ReturnCondition.All.Contains(request.ReturnCondition, StringComparer.Ordinal))
        {
            return Error.Validation(
                "Handover.UnknownCondition",
                $"Condition must be one of {string.Join(", ", ReturnCondition.All)}.");
        }

        var images = request.ImagePaths
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();
        if (images.Count > MaxImages)
        {
            return Error.Validation(
                "Handover.TooManyImages", $"At most {MaxImages} photographs may be attached.");
        }

        var allocation = await db.AssetAllocations
            .SingleOrDefaultAsync(a => a.Id == request.AllocationId, ct);
        if (allocation is null)
        {
            return Error.NotFound("Allocation", request.AllocationId);
        }

        if (allocation.ReturnedOnUtc is not null)
        {
            return Error.Conflict(
                "Allocation.AlreadyReturned", "That allocation was already closed.");
        }

        var handover = new AssetHandover
        {
            AllocationId = allocation.Id,
            AssetId = allocation.AssetId,
            FromEmployeeId = allocation.EmployeeId,
            BranchLocationId = request.BranchLocationId,
            Status = HandoverStatus.HandedOver,
            ReturnCondition = request.ReturnCondition,
            Remarks = request.Remarks,
            HandedOverOnUtc = clock.UtcNow,
            ReceivedByUserId = currentUser.Id,
            IsReceivedByHo = false,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };
        db.AssetHandovers.Add(handover);

        // The handover closes the allocation. Leaving it open would mean the
        // employee still holds an asset that is in a store.
        allocation.ReturnedOnUtc = clock.UtcNow;
        allocation.ReceivedByUserId = currentUser.Id;
        allocation.ModifiedOnUtc = clock.UtcNow;
        allocation.ModifiedBy = currentUser.Username;

        await timeline.AppendAsync(
            new AssetTimelineEntry(
                allocation.AssetId,
                "HandedOver",
                $"Handed into the branch store in {request.ReturnCondition} condition: {request.Remarks}",
                clock.UtcNow,
                currentUser.Username,
                EmployeeId: allocation.EmployeeId,
                LocationId: request.BranchLocationId,
                AllocationId: allocation.Id),
            ct);

        try
        {
            await db.SaveChangesAsync(ct);

            for (var i = 0; i < images.Count; i++)
            {
                db.AssetReturnImages.Add(new AssetReturnImage
                {
                    AllocationId = allocation.Id,
                    HandoverId = handover.Id,
                    ImagePath = images[i],
                    UploadedByUserId = currentUser.Id,
                    CapturedOnUtc = clock.UtcNow,
                });
            }

            if (images.Count > 0)
            {
                await db.SaveChangesAsync(ct);
            }
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

        return new RecordHandoverResponse(handover.Id, handover.Status, images.Count);
    }
}
