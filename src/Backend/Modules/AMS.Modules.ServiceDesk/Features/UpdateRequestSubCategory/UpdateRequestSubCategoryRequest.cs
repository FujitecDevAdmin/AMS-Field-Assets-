namespace AMS.Modules.ServiceDesk.Features.UpdateRequestSubCategory;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateRequestSubCategoryRequest(
    string SubCategoryName,
    bool IsActive);
