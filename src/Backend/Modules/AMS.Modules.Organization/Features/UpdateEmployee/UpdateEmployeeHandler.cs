using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.UpdateEmployee;

/// <summary>
/// Edit an employee. Catalogue: Employee directory, Reporting manager.
/// </summary>
/// <remarks>
/// <para>
/// The first production use of R2-22. The caller's ETag is the
/// <c>ConcurrencyStamp</c> they were given; it becomes the original value, so
/// the UPDATE's WHERE carries it and a stale edit is a 412. A NEW stamp is
/// generated on every successful write, which is what makes the check work:
/// <c>SysStartTime</c> did not move inside a clock tick and lost writes in
/// silence.
/// </para>
/// <para>
/// A management cycle is refused. Two people reporting to each other, directly
/// or through a chain, makes "who approves this?" unanswerable and an org
/// chart that never terminates.
/// </para>
/// </remarks>
public sealed class UpdateEmployeeHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateEmployeeCommand, UpdateEmployeeResponse>
{
    public async Task<Result<UpdateEmployeeResponse>> HandleAsync(
        UpdateEmployeeCommand request,
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

        if (request.ReportingManagerId == request.EmployeeId)
        {
            return Error.Validation(
                "Employee.CannotReportToSelf", "An employee cannot report to themselves.");
        }

        if (request.ReportingManagerId.HasValue)
        {
            var cycle = await WouldCreateCycleAsync(request.EmployeeId, request.ReportingManagerId.Value, ct);
            if (cycle is not null)
            {
                return cycle;
            }
        }

        db.Entry(employee).Property(e => e.ConcurrencyStamp).OriginalValue = expected;

        employee.EmployeeCode = request.EmployeeCode;
        employee.FullName = request.FullName;
        employee.Email = request.Email;
        employee.Phone = request.Phone;
        employee.DepartmentId = request.DepartmentId;
        employee.BranchId = request.BranchId;
        employee.ReportingManagerId = request.ReportingManagerId;

        // The stamp moves because the application changed the row, not because
        // the clock happened to tick (R2-22).
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
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        return new UpdateEmployeeResponse(
            employee.Id, employee.FullName, employee.ConcurrencyStamp.ToString());
    }

    /// <summary>
    /// Walks up from the proposed manager. Arriving back at the employee being
    /// edited means the chain would loop.
    /// </summary>
    private async Task<Error?> WouldCreateCycleAsync(int employeeId, int managerId, CancellationToken ct)
    {
        var chain = await db.Employees
            .AsNoTracking()
            .Select(e => new { e.Id, e.ReportingManagerId })
            .ToDictionaryAsync(e => e.Id, e => e.ReportingManagerId, ct);

        var seen = new HashSet<int> { employeeId };
        int? current = managerId;

        while (current is not null)
        {
            if (!seen.Add(current.Value))
            {
                return Error.Validation(
                    "Employee.ManagementCycle",
                    "That would make two people report to each other, directly or indirectly.");
            }

            current = chain.TryGetValue(current.Value, out var next) ? next : null;
        }

        return null;
    }
}
