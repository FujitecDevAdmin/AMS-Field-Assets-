namespace AMS.Modules.Organization.Features.UpdateDepartment;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateDepartmentMapper
{
    public static UpdateDepartmentCommand ToCommand(UpdateDepartmentRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateDepartmentCommand(
            id,
            request.DepartmentName.Trim(),
            request.IsActive);
    }
}
