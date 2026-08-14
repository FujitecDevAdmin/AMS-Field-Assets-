using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.UpdateRequestCategory;

/// <summary>
/// Rename a category or retire it.
/// </summary>
public sealed record UpdateRequestCategoryCommand(
    int Id,
    string CategoryName,
    bool IsActive) : ICommand<UpdateRequestCategoryResponse>;
