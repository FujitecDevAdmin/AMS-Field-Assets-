using AMS.Modules.Allocations.Persistence;
using AMS.Modules.Assets.PublicApi;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.ReceiveReturn;

/// <summary>
/// Close the allocation and free the asset. Catalogue: Receive a return.
/// </summary>
/// <remarks>
/// Setting ReturnedOnUtc is what releases the asset: the filtered unique index
/// only covers rows where it is null, so the next allocation becomes possible
/// the moment this commits.
/// </remarks>
public sealed class ReceiveReturnHandler(
    AllocationsDbContext db,
    IAssetTimeline timeline,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<ReceiveReturnCommand, ReceiveReturnResponse>
{
    public async Task<Result<ReceiveReturnResponse>> HandleAsync(
        ReceiveReturnCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var allocation = await db.AssetAllocations.SingleOrDefaultAsync(a => a.Id == request.Id, ct);
        if (allocation is null)
        {
            return Error.NotFound("Allocation", request.Id);
        }

        if (allocation.ReturnedOnUtc is not null)
        {
            return Error.Conflict(
                "Allocation.AlreadyReturned", "That allocation was already closed.");
        }

        allocation.ReturnedOnUtc = clock.UtcNow;
        allocation.ReceivedByUserId = currentUser.Id;
        allocation.ModifiedOnUtc = clock.UtcNow;
        allocation.ModifiedBy = currentUser.Username;

        if (!string.IsNullOrWhiteSpace(request.Remarks))
        {
            allocation.Remarks = request.Remarks;
        }

        await timeline.AppendAsync(
            new AssetTimelineEntry(
                allocation.AssetId,
                "Returned",
                request.Remarks is null
                    ? $"Returned by employee {allocation.EmployeeId}."
                    : $"Returned by employee {allocation.EmployeeId}: {request.Remarks}",
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

        return new ReceiveReturnResponse(allocation.Id, allocation.AssetId);
    }
}
