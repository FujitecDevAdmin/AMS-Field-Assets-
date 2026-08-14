using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.CreateAssetStatus;

/// <summary>Add an asset status. Catalogue: Status lookup maintenance.</summary>
public sealed class CreateAssetStatusHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CreateAssetStatusCommand, CreateAssetStatusResponse>
{
    public async Task<Result<CreateAssetStatusResponse>> HandleAsync(
        CreateAssetStatusCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var status = new AssetStatus
        {
            StatusName = request.StatusName,
            IsTerminal = request.IsTerminal,
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.AssetStatuses.Add(status);

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

        return new CreateAssetStatusResponse(status.Id, status.StatusName, status.IsTerminal);
    }
}
