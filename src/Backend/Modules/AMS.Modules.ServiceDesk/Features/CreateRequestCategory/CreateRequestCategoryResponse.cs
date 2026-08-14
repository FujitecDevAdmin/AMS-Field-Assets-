namespace AMS.Modules.ServiceDesk.Features.CreateRequestCategory;

/// <summary>
/// The new category.
/// </summary>
/// <param name="Id">The category.</param>
/// <param name="CategoryName">Unique, trimmed.</param>
public sealed record CreateRequestCategoryResponse(
    int Id,
    string CategoryName);
