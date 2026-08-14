using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.CreateAssetClass;

/// <summary>Add an asset class. Catalogue: Classify an asset for the accounts.</summary>
/// <remarks>
/// <c>IsAuc</c> is deliberately absent from the command. Exactly one class may
/// carry it — <c>UX_AssetClass_OneAuc</c> — because the capitalisation step
/// finds its source class by that flag, and a screen that can set it is a
/// screen that can make capitalisation ambiguous. The seeded AUC class is the
/// only one, and moving it is a migration, not a data entry task.
/// </remarks>
public sealed class CreateAssetClassHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CreateAssetClassCommand, CreateAssetClassResponse>
{
    public async Task<Result<CreateAssetClassResponse>> HandleAsync(
        CreateAssetClassCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var assetClass = new AssetClass
        {
            ClassCode = request.ClassCode,
            ClassName = request.ClassName,
            ReportingCategory = request.ReportingCategory,
            IsDepreciable = request.IsDepreciable,
            IsIntangible = request.IsIntangible,
            IsAuc = false,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.AssetClasses.Add(assetClass);

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

        return new CreateAssetClassResponse(assetClass.Id, assetClass.ClassCode, assetClass.ClassName);
    }
}
