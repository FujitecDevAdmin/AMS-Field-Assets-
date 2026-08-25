namespace AMS.Modules.ServiceDesk.Features.UpdateRequestCategory;

/// <summary>
/// The updated category.
/// </summary>
/// <param name="Id">The category.</param>
/// <param name="CategoryName">Unique, trimmed.</param>
/// <param name="CategoryType">Service or Incident.</param>
/// <param name="IsActive">Retiring hides it from new tickets; existing ones keep it.</param>
public sealed record UpdateRequestCategoryResponse(
    int Id,
    string CategoryName,
    string CategoryType,
    bool IsActive);
