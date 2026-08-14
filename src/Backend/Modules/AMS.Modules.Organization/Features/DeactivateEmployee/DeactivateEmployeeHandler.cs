using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.DeactivateEmployee;

/// <summary>
/// Mark a leaver inactive. Catalogue: "Deactivate a leaver."
/// </summary>
/// <remarks>
/// <para>
/// Never a delete. Assets were allocated to this person, tickets were raised by
/// them and the audit trail names them; removing the row would turn all of that
/// into dangling ids.
/// </para>
/// <para>
/// Anybody who reported to the leaver is detached from them in the same
/// <c>SaveChanges</c>, and the count comes back so the caller knows how many
/// people now need a manager. Leaving direct reports pointing at somebody who
/// has left is how an approval chain quietly stops working.
/// </para>
/// </remarks>
public sealed class DeactivateEmployeeHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser) : IRequestHandler<DeactivateEmployeeCommand, DeactivateEmployeeResponse>
{
    public async Task<Result<DeactivateEmployeeResponse>> HandleAsync(
        DeactivateEmployeeCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var employee = await db.Employees.SingleOrDefaultAsync(e => e.Id == request.EmployeeId, ct);
        if (employee is null)
        {
            return Error.NotFound("Employee", request.EmployeeId);
        }

        if (!Guid.TryParse(request.ETag, out var expected))
        {
            return Error.Validation("Employee.ETagMalformed", "The record version supplied is not valid.");
        }

        db.Entry(employee).Property(e => e.ConcurrencyStamp).OriginalValue = expected;

        var directReports = await db.Employees
            .Where(e => e.ReportingManagerId == request.EmployeeId)
            .ToListAsync(ct);

        foreach (var report in directReports)
        {
            report.ReportingManagerId = null;
            report.ConcurrencyStamp = Guid.NewGuid();
            report.ModifiedOnUtc = clock.UtcNow;
            report.ModifiedBy = currentUser.Username;
        }

        employee.IsActive = false;
        employee.ConcurrencyStamp = Guid.NewGuid();
        employee.ModifiedOnUtc = clock.UtcNow;
        employee.ModifiedBy = currentUser.Username;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Error.Concurrency(
                "Employee.Stale",
                "This record changed while you were editing it. Reload and try again.");
        }

        return new DeactivateEmployeeResponse(employee.Id, employee.IsActive, directReports.Count);
    }
}
