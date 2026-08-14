using AMS.Modules.Verification.Domain;
using AMS.Modules.Verification.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Features.OpenVerificationCycle;

/// <summary>Start a verification round. Catalogue: Verification Cycles.</summary>
/// <remarks>
/// One at a time. <c>UX_PhysicalVerificationCycle_OneActive</c> is a filtered
/// unique index over IsActive = 1, so a second open cycle collides in the
/// database rather than leaving a phone to guess which round its captures
/// belong to — and a phone that guesses wrong has recorded a count against the
/// wrong quarter, which nobody discovers until the numbers are reconciled.
/// </remarks>
public sealed class OpenVerificationCycleHandler(
    VerificationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<OpenVerificationCycleCommand, OpenVerificationCycleResponse>
{
    public async Task<Result<OpenVerificationCycleResponse>> HandleAsync(
        OpenVerificationCycleCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = clock.UtcNow;

        // A cycle that starts on a date nobody chose starts today, which is
        // what an administrator pressing the button means.
        var startDate = request.StartDate == default
            ? DateOnly.FromDateTime(now)
            : request.StartDate;

        if (request.EndDate is { } endDate && endDate < startDate)
        {
            return Error.Validation(
                "VerificationCycle.Window",
                "A cycle cannot end before it starts.");
        }

        var cycle = new PhysicalVerificationCycle
        {
            CycleName = request.CycleName,
            StartDate = startDate,
            EndDate = request.EndDate,
            IsActive = true,
            CreatedOnUtc = now,
            CreatedBy = currentUser.Username,
        };

        db.PhysicalVerificationCycles.Add(cycle);

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

        return new OpenVerificationCycleResponse(cycle.Id, cycle.CycleName, cycle.StartDate);
    }
}
