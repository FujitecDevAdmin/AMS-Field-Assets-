using AMS.Modules.Contracts.Domain;
using AMS.Modules.Contracts.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Contracts.Features.SetContractAssets;

/// <summary>Say what a contract covers. Catalogue: Contract Detail.</summary>
/// <remarks>
/// The whole set at once, for the reason a support team's membership is: add
/// and remove endpoints would make the screen compute a difference against a
/// list that may have moved under it.
/// </remarks>
public sealed class SetContractAssetsHandler(
    ContractsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<SetContractAssetsCommand, SetContractAssetsResponse>
{
    public async Task<Result<SetContractAssetsResponse>> HandleAsync(
        SetContractAssetsCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await db.Contracts.AnyAsync(c => c.Id == request.Id && !c.IsDeleted, ct))
        {
            return Error.NotFound("Contract", request.Id);
        }

        var wanted = request.AssetIds.Distinct().ToList();

        var existing = await db.ContractAssets
            .Where(a => a.ContractId == request.Id)
            .ToListAsync(ct);

        db.ContractAssets.RemoveRange(existing.Where(a => !wanted.Contains(a.AssetId)));

        var now = clock.UtcNow;

        // Kept rather than replaced wholesale, so LinkedOnUtc still says when an
        // asset actually came under cover.
        foreach (var assetId in wanted.Where(id => existing.TrueForAll(a => a.AssetId != id)))
        {
            db.ContractAssets.Add(new ContractAsset
            {
                ContractId = request.Id,
                AssetId = assetId,
                LinkedOnUtc = now,
                LinkedByUserId = currentUser.Id,
            });
        }

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

        return new SetContractAssetsResponse(request.Id, wanted.Count);
    }
}
