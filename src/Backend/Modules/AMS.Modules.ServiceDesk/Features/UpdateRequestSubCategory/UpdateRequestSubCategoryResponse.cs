namespace AMS.Modules.ServiceDesk.Features.UpdateRequestSubCategory;

/// <summary>
/// The updated sub-category.
/// </summary>
/// <param name="Id">The sub-category.</param>
/// <param name="SubCategoryName">Unique within its category.</param>
/// <param name="IsActive">Retiring hides it from new tickets.</param>
public sealed record UpdateRequestSubCategoryResponse(
    int Id,
    string SubCategoryName,
    bool IsActive);
