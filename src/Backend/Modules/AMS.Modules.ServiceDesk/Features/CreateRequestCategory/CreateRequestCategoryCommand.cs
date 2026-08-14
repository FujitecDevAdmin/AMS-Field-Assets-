using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.CreateRequestCategory;

/// <summary>
/// Add a category. Catalogue: Categories and sub-categories.
/// </summary>
public sealed record CreateRequestCategoryCommand(
    string CategoryName) : ICommand<CreateRequestCategoryResponse>;
