namespace AMS.Modules.ServiceDesk.Features.CreateRequestSubCategory;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateRequestSubCategoryMapper
{
    public static CreateRequestSubCategoryCommand ToCommand(CreateRequestSubCategoryRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateRequestSubCategoryCommand(
            id,
            request.SubCategoryName.Trim());
    }
}
