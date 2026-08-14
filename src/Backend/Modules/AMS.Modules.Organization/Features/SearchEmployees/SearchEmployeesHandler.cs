using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.SearchEmployees;

/// <summary>The employee directory. Catalogue screen: Employee Directory.</summary>
/// <remarks>
/// Paged at the database. The directory is the largest table in this module
/// and an unbounded list is a review-blocker (02 §8).
///
/// Branch scoping is NOT applied here. Which employees a Branch Admin may see
/// is decided per request from the caller's branch set, and this handler is
/// not where that lives - a model-level filter would behave differently in the
/// background jobs, where there is no caller at all (schema appendix).
/// </remarks>
public sealed class SearchEmployeesHandler(OrganizationDbContext db)
    : IRequestHandler<SearchEmployeesQuery, SearchEmployeesResponse>
{
    public async Task<Result<SearchEmployeesResponse>> HandleAsync(
        SearchEmployeesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Employees.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search}%";
            query = query.Where(e => EF.Functions.Like(e.FullName, term)
                                  || EF.Functions.Like(e.EmployeeCode, term)
                                  || (e.Email != null && EF.Functions.Like(e.Email, term)));
        }

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == request.DepartmentId.Value);
        }

        if (request.LocationId.HasValue)
        {
            query = query.Where(e => e.LocationId == request.LocationId.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(e => e.IsActive == request.IsActive.Value);
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(e => e.FullName)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(e => new SearchEmployeesResponse.Row(
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Email,
                e.Phone,
                db.Departments.Where(d => d.Id == e.DepartmentId).Select(d => d.DepartmentName).FirstOrDefault(),
                db.Locations.Where(l => l.Id == e.LocationId).Select(l => l.LocationName).FirstOrDefault(),
                db.Employees.Where(m => m.Id == e.ReportingManagerId).Select(m => m.FullName).FirstOrDefault(),
                e.IsActive))
            .ToListAsync(ct);

        return new SearchEmployeesResponse(rows, total);
    }
}
