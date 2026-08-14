using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.SearchServiceTemplates;

/// <summary>Pre-written requests with defaults. Catalogue screen: Service Templates.</summary>
public sealed class SearchServiceTemplatesHandler(ServiceDeskDbContext db)
    : IRequestHandler<SearchServiceTemplatesQuery, SearchServiceTemplatesResponse>
{
    public async Task<Result<SearchServiceTemplatesResponse>> HandleAsync(
        SearchServiceTemplatesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.ServiceTemplates.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.RequestKind))
        {
            query = query.Where(t => t.RequestKind == request.RequestKind);
        }

        var rows = await query
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.TemplateName)
            .Select(t => new SearchServiceTemplatesResponse.Row(
                t.Id, t.TemplateName, t.RequestKind, t.RequestCategoryId, t.RequestSubCategoryId,
                t.DefaultPriority, t.DefaultSupportTeamId, t.SubjectTemplate,
                t.DescriptionTemplate, t.RequiresAsset, t.DisplayOrder, t.IsActive))
            .ToListAsync(ct);

        return new SearchServiceTemplatesResponse(rows);
    }
}
