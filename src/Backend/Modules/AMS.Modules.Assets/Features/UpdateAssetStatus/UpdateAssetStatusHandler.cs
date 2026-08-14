using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.UpdateAssetStatus;

/// <summary>Rename a status, reorder it, or retire it.</summary>
/// <remarks>
/// Retiring a status that assets are currently sitting in is refused. The rows
/// would keep the status — nothing in the database stops that — but the status
/// would vanish from every picker, so the only way back out of it would be a
/// script. A screen that can strand records is worse than one that says no.
/// </remarks>
public sealed class UpdateAssetStatusHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateAssetStatusCommand, UpdateAssetStatusResponse>
{
    public async Task<Result<UpdateAssetStatusResponse>> HandleAsync(
        UpdateAssetStatusCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var status = await db.AssetStatuses.SingleOrDefaultAsync(s => s.Id == request.Id, ct);
        if (status is null)
        {
            return Error.NotFound("AssetStatus", request.Id);
        }

        if (status.IsActive && !request.IsActive)
        {
            var inUse = await db.Assets.CountAsync(
                a => a.AssetStatusId == request.Id && !a.IsDeleted, ct);
            if (inUse > 0)
            {
                return Error.Validation(
                    "AssetStatus.InUse",
                    $"{inUse} asset(s) are in this status. Move them first, then retire it.");
            }
        }

        status.StatusName = request.StatusName;
        status.IsTerminal = request.IsTerminal;
        status.DisplayOrder = request.DisplayOrder;
        status.IsActive = request.IsActive;
        status.ModifiedOnUtc = clock.UtcNow;
        status.ModifiedBy = currentUser.Username;

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

        return new UpdateAssetStatusResponse(status.Id, status.StatusName, status.IsActive);
    }
}
