using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.SearchVendors;

/// <summary>The vendor list. Catalogue: "Suppliers used by purchases and contracts."</summary>
public sealed class SearchVendorsHandler(OrganizationDbContext db)
    : IRequestHandler<SearchVendorsQuery, SearchVendorsResponse>
{
    public async Task<Result<SearchVendorsResponse>> HandleAsync(SearchVendorsQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Vendors.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(v => v.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search}%";
            query = query.Where(v => EF.Functions.Like(v.VendorName, term)
                                  || (v.ContactPerson != null && EF.Functions.Like(v.ContactPerson, term)));
        }

        var rows = await query
            .OrderBy(v => v.VendorName)
            .Select(v => new SearchVendorsResponse.Row(
                v.Id, v.VendorName, v.ContactPerson, v.Phone, v.Email, v.IsActive))
            .ToListAsync(ct);

        return new SearchVendorsResponse(rows);
    }
}
