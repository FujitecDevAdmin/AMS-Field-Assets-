using AMS.Modules.Organization.Domain;
using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.CreateApplication;

/// <summary>Add a business application. Catalogue: Application master.</summary>
public sealed class CreateApplicationHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CreateApplicationCommand, CreateApplicationResponse>
{
    public async Task<Result<CreateApplicationResponse>> HandleAsync(
        CreateApplicationCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = new Application
        {
            ApplicationName = request.ApplicationName,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.Applications.Add(application);

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

        return new CreateApplicationResponse(application.Id, application.ApplicationName);
    }
}
