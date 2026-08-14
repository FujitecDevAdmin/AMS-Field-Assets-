using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.UpdateVendor;

/// <summary>Edit a supplier or retire it. Catalogue screen: Vendors.</summary>
public sealed class UpdateVendorHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateVendorCommand, UpdateVendorResponse>
{
    public async Task<Result<UpdateVendorResponse>> HandleAsync(UpdateVendorCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var vendor = await db.Vendors.SingleOrDefaultAsync(v => v.Id == request.Id, ct);
        if (vendor is null)
        {
            return Error.NotFound("Vendor", request.Id);
        }

        vendor.VendorName = request.VendorName;
        vendor.ContactPerson = request.ContactPerson;
        vendor.Phone = request.Phone;
        vendor.Email = request.Email;
        vendor.IsActive = request.IsActive;
        vendor.ModifiedOnUtc = clock.UtcNow;
        vendor.ModifiedBy = currentUser.Username;

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

        return new UpdateVendorResponse(vendor.Id, vendor.VendorName, vendor.IsActive);
    }
}
