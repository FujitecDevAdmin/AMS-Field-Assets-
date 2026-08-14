using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.RequestReturn;

/// <summary>
/// Tell the branch an asset is ready to give back. Catalogue: Request a return.
/// </summary>
/// <remarks>
/// The employee's own allocation only. Anything else would let one person start
/// returns on another's assets, and the branch queue would fill with requests
/// nobody made.
/// </remarks>
public sealed class RequestReturnHandler(
    AllocationsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<RequestReturnCommand, RequestReturnResponse>
{
    public async Task<Result<RequestReturnResponse>> HandleAsync(
        RequestReturnCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var allocation = await db.AssetAllocations.SingleOrDefaultAsync(a => a.Id == request.Id, ct);
        if (allocation is null)
        {
            return Error.NotFound("Allocation", request.Id);
        }

        // A 404 and not a 403: telling somebody an allocation exists is itself
        // a disclosure about a colleague.
        if (currentUser.EmployeeId != allocation.EmployeeId)
        {
            return Error.NotFound("Allocation", request.Id);
        }

        if (allocation.ReturnedOnUtc is not null)
        {
            return Error.Conflict(
                "Allocation.AlreadyReturned", "That asset has already been given back.");
        }

        // Asking twice is harmless and must not move the timestamp - the branch
        // queue sorts on it, and re-asking would jump the queue.
        if (allocation.ReturnRequestedOnUtc is { } already)
        {
            return new RequestReturnResponse(allocation.Id, already);
        }

        allocation.ReturnRequestedOnUtc = clock.UtcNow;
        allocation.ModifiedOnUtc = clock.UtcNow;
        allocation.ModifiedBy = currentUser.Username;

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

        return new RequestReturnResponse(allocation.Id, allocation.ReturnRequestedOnUtc.Value);
    }
}
