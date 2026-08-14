using AMS.Modules.Verification.Domain;
using AMS.Modules.Verification.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Features.CloseVerificationCycle;

/// <summary>Finish a verification round. Catalogue: Verification Cycles.</summary>
/// <remarks>
/// Closing is what frees the one-active slot for the next round, and it is
/// deliberately separate from the capture capability: a technician walking a
/// branch must not be able to end the count everybody else is still doing.
///
/// Nothing is deleted and nothing is recomputed. The rows recorded against the
/// cycle are what it found, and they stay exactly as the phones sent them.
/// </remarks>
public sealed class CloseVerificationCycleHandler(
    VerificationDbContext db,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<CloseVerificationCycleCommand, CloseVerificationCycleResponse>
{
    public async Task<Result<CloseVerificationCycleResponse>> HandleAsync(
        CloseVerificationCycleCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cycle = await db.PhysicalVerificationCycles
            .SingleOrDefaultAsync(c => c.Id == request.Id, ct);

        if (cycle is null)
        {
            return Error.NotFound("VerificationCycle", request.Id);
        }

        if (!cycle.IsActive)
        {
            return Error.Conflict(
                "VerificationCycle.AlreadyClosed",
                $"{cycle.CycleName} was closed on {cycle.ClosedOnUtc:yyyy-MM-dd}.");
        }

        var now = clock.UtcNow;

        cycle.IsActive = false;
        cycle.ClosedOnUtc = now;
        cycle.ModifiedOnUtc = now;
        cycle.ModifiedBy = currentUser.Username;

        var verified = await db.PhysicalVerifications
            .CountAsync(v => v.PhysicalVerificationCycleId == cycle.Id, ct);

        var exceptions = await db.PhysicalVerifications
            .CountAsync(v => v.PhysicalVerificationCycleId == cycle.Id
                && v.WorkingCondition != WorkingCondition.Good, ct);

        await db.SaveChangesAsync(ct);

        return new CloseVerificationCycleResponse(cycle.Id, verified, exceptions, now);
    }
}
