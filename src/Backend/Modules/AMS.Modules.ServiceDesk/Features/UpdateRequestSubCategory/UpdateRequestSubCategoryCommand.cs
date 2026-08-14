using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.UpdateRequestSubCategory;

/// <summary>
/// Rename a sub-category or retire it.
/// </summary>
public sealed record UpdateRequestSubCategoryCommand(
    int Id,
    string SubCategoryName,
    bool IsActive) : ICommand<UpdateRequestSubCategoryResponse>;
