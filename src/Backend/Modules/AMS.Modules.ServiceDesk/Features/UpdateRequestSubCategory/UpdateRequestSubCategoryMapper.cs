namespace AMS.Modules.ServiceDesk.Features.UpdateRequestSubCategory;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateRequestSubCategoryMapper
{
    public static UpdateRequestSubCategoryCommand ToCommand(UpdateRequestSubCategoryRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateRequestSubCategoryCommand(
            id,
            request.SubCategoryName.Trim(),
            request.IsActive);
    }
}
