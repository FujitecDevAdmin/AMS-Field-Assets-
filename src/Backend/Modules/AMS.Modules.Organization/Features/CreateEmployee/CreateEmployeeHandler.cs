using AMS.Modules.Organization.Domain;
using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.CreateEmployee;

/// <summary>
/// Add somebody to the directory. Catalogue: Employee directory, Reporting
/// manager.
/// </summary>
/// <remarks>
/// The first command in the application to write a system-versioned table. The
/// only thing that differs from an ordinary create is the concurrency token:
/// Employee carries a <c>ConcurrencyStamp</c>, not a rowversion (R2-22).
/// </remarks>
public sealed class CreateEmployeeHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CreateEmployeeCommand, CreateEmployeeResponse>
{
    public async Task<Result<CreateEmployeeResponse>> HandleAsync(
        CreateEmployeeCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // ReportingManagerId is an intra-schema foreign key, so an unknown id
        // would surface as a 500 saying "FK violation". Naming the offending id
        // turns that into something an administrator can act on.
        if (request.ReportingManagerId.HasValue
            && !await db.Employees.AnyAsync(e => e.Id == request.ReportingManagerId.Value, ct))
        {
            return Error.Validation(
                "Employee.ManagerNotFound",
                $"No such employee to report to: {request.ReportingManagerId.Value}.");
        }

        var employee = new Employee
        {
            EmployeeCode = request.EmployeeCode,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            DepartmentId = request.DepartmentId,
            BranchId = request.BranchId,
            ReportingManagerId = request.ReportingManagerId,
            IsActive = true,
            ConcurrencyStamp = Guid.NewGuid(),
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.Employees.Add(employee);

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

        return new CreateEmployeeResponse(
            employee.Id, employee.EmployeeCode, employee.FullName, employee.ConcurrencyStamp.ToString());
    }
}
