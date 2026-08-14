namespace AMS.Modules.ServiceDesk.Features.CreateRequestCategory;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateRequestCategoryMapper
{
    public static CreateRequestCategoryCommand ToCommand(CreateRequestCategoryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateRequestCategoryCommand(
            request.CategoryName.Trim());
    }
}
