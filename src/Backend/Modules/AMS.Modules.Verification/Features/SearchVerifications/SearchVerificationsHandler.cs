using AMS.Modules.Verification.Domain;
using AMS.Modules.Verification.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Features.SearchVerifications;

/// <summary>
/// What was found, and what was not. Catalogue: the exception report.
/// </summary>
/// <remarks>
/// Ordered worst first — Missing above NotWorking above Damaged — because the
/// screen exists to be acted on, and a report that buries the missing assets
/// under three hundred healthy ones is a report nobody finishes reading.
/// </remarks>
public sealed class SearchVerificationsHandler(VerificationDbContext db)
    : IRequestHandler<SearchVerificationsQuery, SearchVerificationsResponse>
{
    public async Task<Result<SearchVerificationsResponse>> HandleAsync(
        SearchVerificationsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.PhysicalVerifications.AsNoTracking();

        if (request.CycleId is { } cycleId)
        {
            query = query.Where(v => v.PhysicalVerificationCycleId == cycleId);
        }

        if (request.LocationId is { } locationId)
        {
            query = query.Where(v => v.LocationId == locationId);
        }

        if (request.WorkingCondition is { } condition)
        {
            query = query.Where(v => v.WorkingCondition == condition);
        }

        if (request.ExceptionsOnly)
        {
            query = query.Where(v => v.WorkingCondition != WorkingCondition.Good);
        }

        if (request.MismatchesOnly)
        {
            query = query.Where(v => v.HasQrMismatch);
        }

        var total = await query.CountAsync(ct);

        var exceptions = await query.CountAsync(
            v => v.WorkingCondition != WorkingCondition.Good, ct);

        var rows = await query
            .OrderBy(v =>
                v.WorkingCondition == WorkingCondition.Missing ? 0
                : v.WorkingCondition == WorkingCondition.NotWorking ? 1
                : v.WorkingCondition == WorkingCondition.Damaged ? 2
                : v.WorkingCondition == WorkingCondition.MinorDamage ? 3
                : 4)
            // A tag on the wrong asset is its own kind of wrong, and it can
            // happen to something in perfect condition.
            .ThenByDescending(v => v.HasQrMismatch)
            .ThenBy(v => v.AssetId)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(v => new SearchVerificationsResponse.Row(
                v.Id,
                v.PhysicalVerificationCycleId,
                v.AssetId,
                v.IsBulkCount,
                v.CountedQuantity,
                v.ExpectedQuantitySnapshot,
                v.CountedQuantity != null && v.ExpectedQuantitySnapshot != null
                    ? v.CountedQuantity - v.ExpectedQuantitySnapshot
                    : null,
                v.WorkingCondition,
                v.HasQrMismatch,
                v.SerialVerified,
                v.LocationId,
                v.HolderEmployeeId,
                v.GpsLatitude,
                v.GpsLongitude,
                v.PhotoPath,
                v.VerifiedByUserId,
                v.VerifiedOnUtc,
                v.Remarks))
            .ToListAsync(ct);

        return new SearchVerificationsResponse(rows, total, exceptions);
    }
}
