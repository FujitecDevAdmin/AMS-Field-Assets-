using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.GetMyAssets;

/// <summary>
/// What the signed-in employee holds. Catalogue screen: My Assets.
/// </summary>
/// <remarks>
/// Reads the employee id off the caller's token and nothing else. No parameter,
/// deliberately: an endpoint that took one would be an endpoint somebody could
/// point at a colleague.
/// </remarks>
public sealed class GetMyAssetsHandler(AllocationsDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyAssetsQuery, GetMyAssetsResponse>
{
    public async Task<Result<GetMyAssetsResponse>> HandleAsync(
        GetMyAssetsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Null is normal - a service account or an administrator outside the
        // directory has a login and no employee record - and the screen must
        // say so rather than show an empty list as though they held nothing.
        if (currentUser.EmployeeId is not { } employeeId)
        {
            return Error.Validation(
                "MyAssets.NoEmployee",
                "This login is not linked to an employee, so it cannot hold assets.");
        }

        var rows = await db.AssetAllocations
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.ReturnedOnUtc == null)
            .OrderByDescending(a => a.AllocatedOnUtc)
            .Select(a => new GetMyAssetsResponse.Row(
                a.Id,
                a.AssetId,
                a.AllocatedOnUtc,
                a.ExpectedReturnDate,
                a.ReturnRequestedOnUtc != null,
                db.AssetAcknowledgements.Where(k => k.AllocationId == a.Id)
                    .Select(k => k.Status).FirstOrDefault()))
            .ToListAsync(ct);

        return new GetMyAssetsResponse(rows);
    }
}
