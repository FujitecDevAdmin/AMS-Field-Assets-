using AMS.Modules.Organization.Domain;
using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.CreateVendor;

/// <summary>Add a supplier. Catalogue screen: Vendors.</summary>
public sealed class CreateVendorHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CreateVendorCommand, CreateVendorResponse>
{
    public async Task<Result<CreateVendorResponse>> HandleAsync(CreateVendorCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var vendor = new Vendor
        {
            VendorName = request.VendorName,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.Vendors.Add(vendor);

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

        return new CreateVendorResponse(vendor.Id, vendor.VendorName);
    }
}
