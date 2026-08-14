namespace AMS.Modules.ServiceDesk.Features.UpdateRequestCategory;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateRequestCategoryMapper
{
    public static UpdateRequestCategoryCommand ToCommand(UpdateRequestCategoryRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateRequestCategoryCommand(
            id,
            request.CategoryName.Trim(),
            request.IsActive);
    }
}
