using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.GetMyApplicationAccess;

/// <summary>
/// What the signed-in employee has been granted. Catalogue: See my application
/// access — "read-only view of what the employee has been granted".
/// </summary>
/// <remarks>
/// <para>
/// Read-only and current-only. An employee has no reason to see what was
/// withdrawn from them, and showing it invites a conversation the screen
/// cannot have.
/// </para>
/// <para>
/// The employee id comes from the caller's claims, resolved at authentication.
/// This module cannot look it up: <c>Identity.User.EmployeeId</c> is another
/// module's table (01 §2 rule 2).
/// </para>
/// </remarks>
public sealed class GetMyApplicationAccessHandler(OrganizationDbContext db)
    : IRequestHandler<GetMyApplicationAccessQuery, GetMyApplicationAccessResponse>
{
    public async Task<Result<GetMyApplicationAccessResponse>> HandleAsync(
        GetMyApplicationAccessQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.EmployeeId is null)
        {
            // A service account, or an administrator who is not in the
            // directory. Empty is the truthful answer, and the null id lets the
            // screen say WHY rather than implying they were granted nothing.
            return new GetMyApplicationAccessResponse(null, []);
        }

        var rows = await db.EmployeeApplications
            .AsNoTracking()
            .Where(ea => ea.EmployeeId == request.EmployeeId.Value && ea.RevokedOnUtc == null)
            .OrderBy(ea => ea.GrantedOnUtc)
            .Select(ea => new GetMyApplicationAccessResponse.Row(
                ea.ApplicationId,
                db.Applications.Where(a => a.Id == ea.ApplicationId)
                    .Select(a => a.ApplicationName).FirstOrDefault() ?? string.Empty,
                ea.ApplicationLoginId,
                ea.GrantedOnUtc))
            .ToListAsync(ct);

        return new GetMyApplicationAccessResponse(request.EmployeeId, rows);
    }
}
