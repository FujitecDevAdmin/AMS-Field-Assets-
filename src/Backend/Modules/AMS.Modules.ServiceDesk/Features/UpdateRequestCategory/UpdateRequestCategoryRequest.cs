namespace AMS.Modules.ServiceDesk.Features.UpdateRequestCategory;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateRequestCategoryRequest(
    string CategoryName,
    string CategoryType,
    bool IsActive);
