using AMS.Modules.Organization.Domain;
using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.GrantApplicationAccess;

/// <summary>
/// Record that an employee may use an application. Catalogue: Grant and revoke
/// application access.
/// </summary>
/// <remarks>
/// <c>UX_EmployeeApplication_OneActive</c> is filtered on
/// <c>RevokedOnUtc IS NULL</c>, which is what makes this work: the same pair
/// cannot be granted twice while it is held, but it can be granted again after
/// it was withdrawn. Both halves matter - people rejoin teams.
/// </remarks>
public sealed class GrantApplicationAccessHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<GrantApplicationAccessCommand, GrantApplicationAccessResponse>
{
    public async Task<Result<GrantApplicationAccessResponse>> HandleAsync(
        GrantApplicationAccessCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await db.Employees.AnyAsync(e => e.Id == request.EmployeeId, ct))
        {
            return Error.NotFound("Employee", request.EmployeeId);
        }

        if (!await db.Applications.AnyAsync(a => a.Id == request.ApplicationId, ct))
        {
            return Error.NotFound("Application", request.ApplicationId);
        }

        var grant = new EmployeeApplication
        {
            EmployeeId = request.EmployeeId,
            ApplicationId = request.ApplicationId,
            ApplicationLoginId = request.ApplicationLoginId,
            GrantedOnUtc = clock.UtcNow,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.EmployeeApplications.Add(grant);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            // The filtered index decides, not a read-then-write check.
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        return new GrantApplicationAccessResponse(
            grant.Id, grant.EmployeeId, grant.ApplicationId, grant.GrantedOnUtc);
    }
}
