using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.UpdateRequestCategory;

/// <summary>Rename a category or retire it.</summary>
/// <remarks>
/// Retiring a category retires nothing beneath it. A sub-category whose parent
/// is inactive is already unreachable from the raise screen, and cascading the
/// flag would make reactivating the parent silently resurrect sub-categories
/// somebody had retired on purpose.
/// </remarks>
public sealed class UpdateRequestCategoryHandler(
    ServiceDeskDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<UpdateRequestCategoryCommand, UpdateRequestCategoryResponse>
{
    public async Task<Result<UpdateRequestCategoryResponse>> HandleAsync(
        UpdateRequestCategoryCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = await db.RequestCategories.SingleOrDefaultAsync(c => c.Id == request.Id, ct);
        if (category is null)
        {
            return Error.NotFound("RequestCategory", request.Id);
        }

        category.CategoryName = request.CategoryName;
        category.IsActive = request.IsActive;
        category.ModifiedOnUtc = clock.UtcNow;
        category.ModifiedBy = currentUser.Username;

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

        return new UpdateRequestCategoryResponse(
            category.Id, category.CategoryName, category.IsActive);
    }
}
