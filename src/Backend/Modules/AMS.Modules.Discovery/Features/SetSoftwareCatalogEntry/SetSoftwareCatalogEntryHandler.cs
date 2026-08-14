using AMS.Modules.Discovery.Domain;
using AMS.Modules.Discovery.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Discovery.Features.SetSoftwareCatalogEntry;

/// <summary>
/// Record what we are licensed for. Catalogue: Software Catalogue.
/// </summary>
/// <remarks>
/// An upsert keyed on the name, because the name is what the agent reports and
/// there is nothing else to match on. <c>UX_SoftwareCatalog_Name</c> makes that
/// safe: two administrators cataloguing the same title at once collide rather
/// than producing two entries that disagree about how many seats we own.
/// </remarks>
public sealed class SetSoftwareCatalogEntryHandler(
    DiscoveryDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<SetSoftwareCatalogEntryCommand, SetSoftwareCatalogEntryResponse>
{
    public async Task<Result<SetSoftwareCatalogEntryResponse>> HandleAsync(
        SetSoftwareCatalogEntryCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = clock.UtcNow;

        var entry = await db.SoftwareCatalogs
            .SingleOrDefaultAsync(c => c.SoftwareName == request.SoftwareName, ct);

        if (entry is null)
        {
            entry = new SoftwareCatalog
            {
                SoftwareName = request.SoftwareName,
                CreatedOnUtc = now,
                CreatedBy = currentUser.Username,
            };

            db.SoftwareCatalogs.Add(entry);
        }
        else
        {
            entry.ModifiedOnUtc = now;
            entry.ModifiedBy = currentUser.Username;
        }

        entry.Publisher = request.Publisher;
        entry.LicensedSeats = request.LicensedSeats;
        entry.ContractId = request.ContractId;
        entry.IsBlacklisted = request.IsBlacklisted;
        entry.IsActive = request.IsActive;

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

        // By distinct machine, for the same reason the report counts that way.
        var installed = await db.AssetInstalledSoftwares
            .Where(s => s.SoftwareName == request.SoftwareName && !s.IsRemoved)
            .Select(s => s.AssetId)
            .Distinct()
            .CountAsync(ct);

        return new SetSoftwareCatalogEntryResponse(
            entry.Id,
            entry.SoftwareName,
            entry.LicensedSeats,
            installed,
            entry.LicensedSeats is { } seats && installed > seats);
    }
}
