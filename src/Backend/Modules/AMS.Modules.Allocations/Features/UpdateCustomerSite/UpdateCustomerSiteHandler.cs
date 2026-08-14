using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.UpdateCustomerSite;

/// <summary>Edit a customer site or retire it.</summary>
/// <remarks>
/// Retiring a site that still has assets on it is refused. The mappings would
/// survive — nothing in the database stops that — but the site would vanish
/// from every picker, so the only way to get those assets off it would be a
/// script.
/// </remarks>
public sealed class UpdateCustomerSiteHandler(
    AllocationsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateCustomerSiteCommand, UpdateCustomerSiteResponse>
{
    public async Task<Result<UpdateCustomerSiteResponse>> HandleAsync(
        UpdateCustomerSiteCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var site = await db.CustomerSites.SingleOrDefaultAsync(s => s.Id == request.Id, ct);
        if (site is null)
        {
            return Error.NotFound("CustomerSite", request.Id);
        }

        if (site.IsActive && !request.IsActive)
        {
            var onSite = await db.AssetSiteMappings
                .CountAsync(m => m.CustomerSiteId == request.Id && m.RemovedOnUtc == null, ct);
            if (onSite > 0)
            {
                return Error.Validation(
                    "CustomerSite.InUse",
                    $"{onSite} asset(s) are still at this site. Take them off it first.");
            }
        }

        site.CustomerName = request.CustomerName;
        site.SiteName = request.SiteName;
        site.City = request.City;
        site.Address = request.Address;
        site.Latitude = request.Latitude;
        site.Longitude = request.Longitude;
        site.IsActive = request.IsActive;
        site.ModifiedOnUtc = clock.UtcNow;
        site.ModifiedBy = currentUser.Username;

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

        return new UpdateCustomerSiteResponse(site.Id, site.SiteName, site.IsActive);
    }
}
