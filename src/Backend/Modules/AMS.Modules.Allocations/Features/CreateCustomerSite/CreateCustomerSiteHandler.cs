using AMS.Modules.Allocations.Domain;
using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.CreateCustomerSite;

/// <summary>Add a customer site.</summary>
public sealed class CreateCustomerSiteHandler(
    AllocationsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CreateCustomerSiteCommand, CreateCustomerSiteResponse>
{
    public async Task<Result<CreateCustomerSiteResponse>> HandleAsync(
        CreateCustomerSiteCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var site = new CustomerSite
        {
            CustomerName = request.CustomerName,
            SiteName = request.SiteName,
            City = request.City,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.CustomerSites.Add(site);

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

        return new CreateCustomerSiteResponse(site.Id, site.SiteName);
    }
}
