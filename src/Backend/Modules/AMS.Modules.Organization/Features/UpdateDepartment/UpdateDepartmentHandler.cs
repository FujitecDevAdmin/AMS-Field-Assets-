using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.UpdateDepartment;

/// <summary>
/// Rename a department or deactivate it. Catalogue: "Create, rename,
/// deactivate."
/// </summary>
public sealed class UpdateDepartmentHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateDepartmentCommand, UpdateDepartmentResponse>
{
    public async Task<Result<UpdateDepartmentResponse>> HandleAsync(
        UpdateDepartmentCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var department = await db.Departments.SingleOrDefaultAsync(d => d.Id == request.Id, ct);
        if (department is null)
        {
            return Error.NotFound("Department", request.Id);
        }

        department.DepartmentName = request.DepartmentName;
        department.IsActive = request.IsActive;
        department.ModifiedOnUtc = clock.UtcNow;
        department.ModifiedBy = currentUser.Username;

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

        return new UpdateDepartmentResponse(department.Id, department.DepartmentName, department.IsActive);
    }
}
