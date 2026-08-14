using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.CreateRequestSubCategory;

/// <summary>
/// Add a sub-category under a category.
/// </summary>
public sealed record CreateRequestSubCategoryCommand(
    int RequestCategoryId,
    string SubCategoryName) : ICommand<CreateRequestSubCategoryResponse>;
