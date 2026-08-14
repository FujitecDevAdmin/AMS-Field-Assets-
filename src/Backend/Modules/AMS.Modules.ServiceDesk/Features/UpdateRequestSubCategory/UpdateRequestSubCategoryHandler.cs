using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.UpdateRequestSubCategory;

/// <summary>Rename a sub-category or retire it.</summary>
/// <remarks>
/// It cannot be moved to another category. Tickets classified under it mean
/// "this kind of problem, in that area", and re-parenting would silently
/// rewrite what every one of them said.
/// </remarks>
public sealed class UpdateRequestSubCategoryHandler(
    ServiceDeskDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<UpdateRequestSubCategoryCommand, UpdateRequestSubCategoryResponse>
{
    public async Task<Result<UpdateRequestSubCategoryResponse>> HandleAsync(
        UpdateRequestSubCategoryCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subCategory = await db.RequestSubCategories
            .SingleOrDefaultAsync(s => s.Id == request.Id, ct);
        if (subCategory is null)
        {
            return Error.NotFound("RequestSubCategory", request.Id);
        }

        subCategory.SubCategoryName = request.SubCategoryName;
        subCategory.IsActive = request.IsActive;
        subCategory.ModifiedOnUtc = clock.UtcNow;
        subCategory.ModifiedBy = currentUser.Username;

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

        return new UpdateRequestSubCategoryResponse(
            subCategory.Id, subCategory.SubCategoryName, subCategory.IsActive);
    }
}
