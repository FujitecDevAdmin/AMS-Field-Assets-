namespace AMS.Modules.ServiceDesk.Features.CreateRequestSubCategory;

/// <summary>
/// The new sub-category.
/// </summary>
/// <param name="Id">The sub-category.</param>
/// <param name="RequestCategoryId">Its parent.</param>
/// <param name="SubCategoryName">Unique WITHIN the category, not globally — 'Hardware' can sit under two.</param>
public sealed record CreateRequestSubCategoryResponse(
    int Id,
    int RequestCategoryId,
    string SubCategoryName);
