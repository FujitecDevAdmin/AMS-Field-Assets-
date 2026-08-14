using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.GetCapabilities;

/// <summary>
/// The capability catalogue. Read-only: capabilities are registered by the
/// schema's seed, not created through the UI, because an endpoint has to
/// declare one before it can mean anything.
/// </summary>
public sealed class GetCapabilitiesHandler(IdentityDbContext db)
    : IRequestHandler<GetCapabilitiesQuery, GetCapabilitiesResponse>
{
    public async Task<Result<GetCapabilitiesResponse>> HandleAsync(
        GetCapabilitiesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Capabilities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Module))
        {
            query = query.Where(c => c.Module == request.Module);
        }

        var rows = await query
            .OrderBy(c => c.Module)
            .ThenBy(c => c.Name)
            .Select(c => new GetCapabilitiesResponse.Row(c.Name, c.Module, c.Description))
            .ToListAsync(ct);

        return new GetCapabilitiesResponse(rows);
    }
}
