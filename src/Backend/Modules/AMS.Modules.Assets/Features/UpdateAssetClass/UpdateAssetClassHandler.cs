using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.UpdateAssetClass;

/// <summary>Edit an asset class or retire it.</summary>
/// <remarks>
/// <c>IsAuc</c> is not editable here, for the reason given on
/// <c>CreateAssetClassHandler</c>. Retiring the AUC class is refused outright:
/// capitalisation reads it by flag, and an inactive one would leave every
/// asset under construction with nowhere to settle from.
/// </remarks>
public sealed class UpdateAssetClassHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateAssetClassCommand, UpdateAssetClassResponse>
{
    public async Task<Result<UpdateAssetClassResponse>> HandleAsync(
        UpdateAssetClassCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var assetClass = await db.AssetClasses.SingleOrDefaultAsync(c => c.Id == request.Id, ct);
        if (assetClass is null)
        {
            return Error.NotFound("AssetClass", request.Id);
        }

        if (assetClass.IsAuc && !request.IsActive)
        {
            return Error.Validation(
                "AssetClass.AucMustStayActive",
                "The assets-under-construction class cannot be retired: capitalisation depends on it.");
        }

        assetClass.ClassCode = request.ClassCode;
        assetClass.ClassName = request.ClassName;
        assetClass.ReportingCategory = request.ReportingCategory;
        assetClass.IsDepreciable = request.IsDepreciable;
        assetClass.IsIntangible = request.IsIntangible;
        assetClass.IsActive = request.IsActive;
        assetClass.ModifiedOnUtc = clock.UtcNow;
        assetClass.ModifiedBy = currentUser.Username;

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

        return new UpdateAssetClassResponse(assetClass.Id, assetClass.ClassCode, assetClass.IsActive);
    }
}
