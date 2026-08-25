using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.SearchRequestCategories;

/// <summary>
/// The two-level classification. Catalogue screen: Categories.
/// </summary>
public sealed record SearchRequestCategoriesQuery(
    bool? IsActive,
    string? CategoryType,
    bool ActiveSubCategoriesOnly) : IQuery<SearchRequestCategoriesResponse>;
