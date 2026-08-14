using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.GetEmployeeApplications;

/// <summary>
/// What one employee has been granted. Catalogue screen: Applications and
/// Access.
/// </summary>
public sealed class GetEmployeeApplicationsHandler(OrganizationDbContext db)
    : IRequestHandler<GetEmployeeApplicationsQuery, GetEmployeeApplicationsResponse>
{
    public async Task<Result<GetEmployeeApplicationsResponse>> HandleAsync(
        GetEmployeeApplicationsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await db.Employees.AnyAsync(e => e.Id == request.EmployeeId, ct))
        {
            return Error.NotFound("Employee", request.EmployeeId);
        }

        var query = db.EmployeeApplications
            .AsNoTracking()
            .Where(ea => ea.EmployeeId == request.EmployeeId);

        if (!request.IncludeRevoked)
        {
            query = query.Where(ea => ea.RevokedOnUtc == null);
        }

        var rows = await query
            .OrderBy(ea => ea.GrantedOnUtc)
            .Select(ea => new GetEmployeeApplicationsResponse.Row(
                ea.Id,
                ea.ApplicationId,
                db.Applications.Where(a => a.Id == ea.ApplicationId)
                    .Select(a => a.ApplicationName).FirstOrDefault() ?? string.Empty,
                ea.ApplicationLoginId,
                ea.GrantedOnUtc,
                ea.RevokedOnUtc))
            .ToListAsync(ct);

        return new GetEmployeeApplicationsResponse(request.EmployeeId, rows);
    }
}
