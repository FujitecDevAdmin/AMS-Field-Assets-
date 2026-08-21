using AMS.Modules.Verification.Domain;
using AMS.Modules.Verification.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Features.SearchVerificationCycles;

/// <summary>The verification cycles. Catalogue: Verification Cycles.</summary>
/// <remarks>
/// Each with its totals, because the question anybody opens this screen with is
/// how far through the count they are.
/// </remarks>
public sealed class SearchVerificationCyclesHandler(VerificationDbContext db)
    : IRequestHandler<SearchVerificationCyclesQuery, SearchVerificationCyclesResponse>
{
    public async Task<Result<SearchVerificationCyclesResponse>> HandleAsync(
        SearchVerificationCyclesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.PhysicalVerificationCycles.AsNoTracking();

        if (request.ActiveOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        var rows = await query
            .OrderByDescending(c => c.StartDate)
            .ThenByDescending(c => c.Id)
            .Select(c => new SearchVerificationCyclesResponse.Row(
                c.Id,
                c.CycleName,
                c.BranchId,
                c.StartDate,
                c.EndDate,
                c.IsActive,
                c.ClosedOnUtc,
                c.TotalAssetCount,
                db.PhysicalVerificationAssignments
                    .Where(a => a.PhysicalVerificationCycleId == c.Id)
                    .Select(a => a.AuditorUserId).ToList(),
                db.PhysicalVerificationCycleLocations
                    .Where(l => l.PhysicalVerificationCycleId == c.Id)
                    .Select(l => l.BranchId).ToList(),
                db.PhysicalVerifications.Count(v => v.PhysicalVerificationCycleId == c.Id),
                db.PhysicalVerifications.Count(v =>
                    v.PhysicalVerificationCycleId == c.Id
                    && v.WorkingCondition != WorkingCondition.Good)))
            .ToListAsync(ct);

        return new SearchVerificationCyclesResponse(rows);
    }
}
