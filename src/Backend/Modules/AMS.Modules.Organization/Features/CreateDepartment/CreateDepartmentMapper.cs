namespace AMS.Modules.Organization.Features.CreateDepartment;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateDepartmentMapper
{
    public static CreateDepartmentCommand ToCommand(CreateDepartmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateDepartmentCommand(
            request.DepartmentName.Trim());
    }
}
