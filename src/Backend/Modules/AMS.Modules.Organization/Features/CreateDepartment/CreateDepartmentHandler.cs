using AMS.Modules.Organization.Domain;
using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.CreateDepartment;

/// <summary>Add a department. Catalogue: Departments.</summary>
public sealed class CreateDepartmentHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CreateDepartmentCommand, CreateDepartmentResponse>
{
    public async Task<Result<CreateDepartmentResponse>> HandleAsync(
        CreateDepartmentCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var department = new Department
        {
            DepartmentName = request.DepartmentName,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.Departments.Add(department);

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

        return new CreateDepartmentResponse(department.Id, department.DepartmentName);
    }
}
