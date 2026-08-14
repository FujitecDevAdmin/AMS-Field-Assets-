using AMS.Modules.Allocations.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Features.RemoveAssetFromSite;

/// <summary>Take an asset off a customer site.</summary>
/// <remarks>
/// Stamps RemovedOnUtc rather than deleting. The row is the record that the
/// asset WAS there, which is the question anybody asks six months later; and
/// the filtered unique index only covers live mappings, so stamping it is also
/// what frees the asset to be mapped somewhere else.
/// </remarks>
public sealed class RemoveAssetFromSiteHandler(
    AllocationsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<RemoveAssetFromSiteCommand, RemoveAssetFromSiteResponse>
{
    public async Task<Result<RemoveAssetFromSiteResponse>> HandleAsync(
        RemoveAssetFromSiteCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mapping = await db.AssetSiteMappings.SingleOrDefaultAsync(m => m.Id == request.Id, ct);
        if (mapping is null)
        {
            return Error.NotFound("AssetSiteMapping", request.Id);
        }

        // Removing twice is harmless, but it must not move the date - somebody
        // may already have reported on when it came off site.
        if (mapping.RemovedOnUtc is { } already)
        {
            return new RemoveAssetFromSiteResponse(mapping.Id, already);
        }

        mapping.RemovedOnUtc = clock.UtcNow;
        mapping.ModifiedOnUtc = clock.UtcNow;
        mapping.ModifiedBy = currentUser.Username;

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

        return new RemoveAssetFromSiteResponse(mapping.Id, mapping.RemovedOnUtc.Value);
    }
}
