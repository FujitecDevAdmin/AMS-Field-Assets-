using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.UpdateApplication;

/// <summary>Rename an application or retire it. Catalogue: Application master.</summary>
public sealed class UpdateApplicationHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateApplicationCommand, UpdateApplicationResponse>
{
    public async Task<Result<UpdateApplicationResponse>> HandleAsync(
        UpdateApplicationCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await db.Applications.SingleOrDefaultAsync(a => a.Id == request.Id, ct);
        if (application is null)
        {
            return Error.NotFound("Application", request.Id);
        }

        application.ApplicationName = request.ApplicationName;
        application.IsActive = request.IsActive;
        application.ModifiedOnUtc = clock.UtcNow;
        application.ModifiedBy = currentUser.Username;

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

        return new UpdateApplicationResponse(
            application.Id, application.ApplicationName, application.IsActive);
    }
}
