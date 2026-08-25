namespace AMS.Modules.ServiceDesk.Features.CreateRequestCategory;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateRequestCategoryRequest(
    string CategoryName,
    string CategoryType);
