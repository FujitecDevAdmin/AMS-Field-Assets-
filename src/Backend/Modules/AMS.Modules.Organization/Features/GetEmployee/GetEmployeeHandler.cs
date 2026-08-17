using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.GetEmployee;

/// <summary>One employee, as the directory form edits them.</summary>
public sealed class GetEmployeeHandler(OrganizationDbContext db)
    : IRequestHandler<GetEmployeeQuery, GetEmployeeResponse>
{
    public async Task<Result<GetEmployeeResponse>> HandleAsync(GetEmployeeQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var employee = await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == request.EmployeeId)
            .Select(e => new GetEmployeeResponse(
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Email,
                e.Phone,
                e.DepartmentId,
                db.Departments.Where(d => d.Id == e.DepartmentId).Select(d => d.DepartmentName).FirstOrDefault(),
                e.BranchId,
                db.Branches.Where(l => l.Id == e.BranchId).Select(l => l.BranchName).FirstOrDefault(),
                e.ReportingManagerId,
                db.Employees.Where(m => m.Id == e.ReportingManagerId).Select(m => m.FullName).FirstOrDefault(),
                e.IsActive,

                // R2-22: the token for a system-versioned table.
                e.ConcurrencyStamp.ToString()))
            .SingleOrDefaultAsync(ct);

        return employee is null
            ? Error.NotFound("Employee", request.EmployeeId)
            : employee;
    }
}
