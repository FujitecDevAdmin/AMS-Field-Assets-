using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.SearchDepartments;

/// <summary>The department list. Catalogue screen: Departments.</summary>
public sealed class SearchDepartmentsHandler(OrganizationDbContext db)
    : IRequestHandler<SearchDepartmentsQuery, SearchDepartmentsResponse>
{
    public async Task<Result<SearchDepartmentsResponse>> HandleAsync(
        SearchDepartmentsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Departments.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(d => d.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search}%";
            query = query.Where(d => EF.Functions.Like(d.DepartmentName, term));
        }

        var rows = await query
            .OrderBy(d => d.DepartmentName)
            .Select(d => new SearchDepartmentsResponse.Row(
                d.Id,
                d.DepartmentName,
                d.IsActive,
                db.Employees.Count(e => e.DepartmentId == d.Id)))
            .ToListAsync(ct);

        return new SearchDepartmentsResponse(rows);
    }
}
