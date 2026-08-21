using AMS.Modules.Verification.Domain;
using AMS.Modules.Verification.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Features.AddAuditorsToCycle;

public sealed class AddAuditorsToCycleHandler(
    VerificationDbContext db,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<AddAuditorsToCycleCommand, AddAuditorsToCycleResponse>
{
    public async Task<Result<AddAuditorsToCycleResponse>> HandleAsync(
        AddAuditorsToCycleCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cycle = await db.PhysicalVerificationCycles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.CycleId, ct);
        if (cycle is null)
        {
            return Error.NotFound("VerificationCycle", request.CycleId);
        }
        if (!cycle.IsActive)
        {
            return Error.Conflict(
                "VerificationCycle.NotActive",
                "Auditors can only be added while an audit is active or in progress.");
        }

        var requestedIds = request.AuditorUserIds.Distinct().ToArray();
        var existingIds = await db.PhysicalVerificationAssignments
            .Where(item => item.PhysicalVerificationCycleId == request.CycleId)
            .Select(item => item.AuditorUserId)
            .ToListAsync(ct);
        var existingSet = existingIds.ToHashSet();
        var addedIds = requestedIds.Where(id => !existingSet.Contains(id)).ToArray();
        if (addedIds.Length == 0)
        {
            return Error.Conflict(
                "VerificationCycle.AssignmentExists",
                "The selected auditors are already assigned to this audit.");
        }

        var now = clock.UtcNow;
        db.PhysicalVerificationAssignments.AddRange(addedIds.Select(auditorUserId =>
            new PhysicalVerificationAssignment
            {
                PhysicalVerificationCycleId = request.CycleId,
                AuditorUserId = auditorUserId,
                AssignedOnUtc = now,
                AssignedBy = currentUser.Username,
            }));
        await db.SaveChangesAsync(ct);

        return new AddAuditorsToCycleResponse(
            request.CycleId,
            addedIds,
            existingIds.Concat(addedIds).Distinct().ToArray());
    }
}
