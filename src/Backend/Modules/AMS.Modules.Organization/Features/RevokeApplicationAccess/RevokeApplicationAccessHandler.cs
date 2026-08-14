using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.RevokeApplicationAccess;

/// <summary>
/// Withdraw an employee's access. Catalogue: "and withdraw it later".
/// </summary>
/// <remarks>
/// Stamping <c>RevokedOnUtc</c> rather than deleting the row does two things:
/// it takes the row out of the filtered unique index so access can be granted
/// again later, and it keeps the record that access WAS held - which is
/// precisely what an audit asks about after somebody leaves.
/// </remarks>
public sealed class RevokeApplicationAccessHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<RevokeApplicationAccessCommand, RevokeApplicationAccessResponse>
{
    public async Task<Result<RevokeApplicationAccessResponse>> HandleAsync(
        RevokeApplicationAccessCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grant = await db.EmployeeApplications.SingleOrDefaultAsync(
            ea => ea.EmployeeId == request.EmployeeId
               && ea.ApplicationId == request.ApplicationId
               && ea.RevokedOnUtc == null,
            ct);

        if (grant is null)
        {
            // Either never granted, or already withdrawn. The caller wanted the
            // access gone and it is gone; saying "not found" is honest about
            // there being nothing to revoke.
            return Error.NotFound("ApplicationAccess", $"{request.EmployeeId}/{request.ApplicationId}");
        }

        grant.RevokedOnUtc = clock.UtcNow;
        grant.ModifiedOnUtc = clock.UtcNow;
        grant.ModifiedBy = currentUser.Username;

        await db.SaveChangesAsync(ct);

        return new RevokeApplicationAccessResponse(grant.Id, grant.RevokedOnUtc.Value);
    }
}
