using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.SearchAssetClasses;

/// <summary>
/// The finance taxonomy. Catalogue screen: Asset Classes and Chart of Accounts.
/// </summary>
/// <remarks>
/// Thirteen rows on the live register, so no paging and no search box.
/// </remarks>
public sealed class SearchAssetClassesHandler(AssetsDbContext db)
    : IRequestHandler<SearchAssetClassesQuery, SearchAssetClassesResponse>
{
    public async Task<Result<SearchAssetClassesResponse>> HandleAsync(
        SearchAssetClassesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.AssetClasses.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == request.IsActive.Value);
        }

        var rows = await query
            .OrderBy(c => c.ClassCode)
            .Select(c => new SearchAssetClassesResponse.Row(
                c.Id,
                c.ClassCode,
                c.ClassName,
                c.ReportingCategory,
                c.IsDepreciable,
                c.IsIntangible,
                c.IsAuc,
                c.IsActive,
                db.Assets.Count(a => a.AssetClassId == c.Id && !a.IsDeleted)))
            .ToListAsync(ct);

        return new SearchAssetClassesResponse(rows);
    }
}
