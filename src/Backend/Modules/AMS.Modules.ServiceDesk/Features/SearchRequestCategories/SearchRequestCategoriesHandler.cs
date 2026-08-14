using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.SearchRequestCategories;

/// <summary>The two-level classification. Catalogue screen: Categories.</summary>
/// <remarks>
/// Categories and their sub-categories in one round trip, because the screen is
/// a tree and cannot draw the top of it without the bottom.
/// </remarks>
public sealed class SearchRequestCategoriesHandler(ServiceDeskDbContext db)
    : IRequestHandler<SearchRequestCategoriesQuery, SearchRequestCategoriesResponse>
{
    public async Task<Result<SearchRequestCategoriesResponse>> HandleAsync(
        SearchRequestCategoriesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.RequestCategories.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == request.IsActive.Value);
        }

        var rows = await query
            .OrderBy(c => c.CategoryName)
            .Select(c => new SearchRequestCategoriesResponse.Row(
                c.Id,
                c.CategoryName,
                c.IsActive,
                db.ServiceRequests.Count(r => r.RequestCategoryId == c.Id),
                db.RequestSubCategories
                    .Where(s => s.RequestCategoryId == c.Id)
                    .OrderBy(s => s.SubCategoryName)
                    .Select(s => new SearchRequestCategoriesResponse.SubCategoryRow(
                        s.Id, s.SubCategoryName, s.IsActive))
                    .ToList()))
            .ToListAsync(ct);

        return new SearchRequestCategoriesResponse(rows);
    }
}
