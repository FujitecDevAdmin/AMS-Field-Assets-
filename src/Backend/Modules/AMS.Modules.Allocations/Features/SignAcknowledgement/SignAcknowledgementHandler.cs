using AMS.Modules.Allocations.Domain;
using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.SignAcknowledgement;

/// <summary>
/// A digital signature on the undertaking. Catalogue: Sign for an asset.
/// </summary>
/// <remarks>
/// The holder signs, nobody else. An administrator who could sign on somebody's
/// behalf would make the signature worthless as evidence, which is the only
/// thing it is for.
/// </remarks>
public sealed class SignAcknowledgementHandler(
    AllocationsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<SignAcknowledgementCommand, SignAcknowledgementResponse>
{
    public async Task<Result<SignAcknowledgementResponse>> HandleAsync(
        SignAcknowledgementCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var allocation = await db.AssetAllocations
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == request.AllocationId, ct);
        if (allocation is null)
        {
            return Error.NotFound("Allocation", request.AllocationId);
        }

        if (currentUser.EmployeeId != allocation.EmployeeId)
        {
            return Error.NotFound("Allocation", request.AllocationId);
        }

        var acknowledgement = await db.AssetAcknowledgements
            .SingleOrDefaultAsync(k => k.AllocationId == request.AllocationId, ct);
        if (acknowledgement is null)
        {
            return Error.NotFound("Acknowledgement", request.AllocationId);
        }

        // Signing again would move SignedOnUtc, and the manager may already have
        // countersigned what was there.
        if (acknowledgement.Status != AcknowledgementStatus.Pending)
        {
            return Error.Conflict(
                "Acknowledgement.AlreadySigned", "That asset has already been signed for.");
        }

        acknowledgement.Status = AcknowledgementStatus.Signed;
        acknowledgement.SignatureImagePath = request.SignatureImagePath;
        acknowledgement.DocumentPath = request.DocumentPath;
        acknowledgement.SignedOnUtc = clock.UtcNow;
        acknowledgement.ModifiedOnUtc = clock.UtcNow;
        acknowledgement.ModifiedBy = currentUser.Username;

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

        return new SignAcknowledgementResponse(acknowledgement.Id, acknowledgement.Status);
    }
}
