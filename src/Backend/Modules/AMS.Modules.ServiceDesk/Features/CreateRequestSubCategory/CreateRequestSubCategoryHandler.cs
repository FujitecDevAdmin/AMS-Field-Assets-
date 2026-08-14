using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.CreateRequestSubCategory;

/// <summary>Add a sub-category under a category.</summary>
public sealed class CreateRequestSubCategoryHandler(
    ServiceDeskDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<CreateRequestSubCategoryCommand, CreateRequestSubCategoryResponse>
{
    public async Task<Result<CreateRequestSubCategoryResponse>> HandleAsync(
        CreateRequestSubCategoryCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await db.RequestCategories.AnyAsync(c => c.Id == request.RequestCategoryId, ct))
        {
            return Error.NotFound("RequestCategory", request.RequestCategoryId);
        }

        var subCategory = new RequestSubCategory
        {
            RequestCategoryId = request.RequestCategoryId,
            SubCategoryName = request.SubCategoryName,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.RequestSubCategories.Add(subCategory);

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

        return new CreateRequestSubCategoryResponse(
            subCategory.Id, subCategory.RequestCategoryId, subCategory.SubCategoryName);
    }
}
