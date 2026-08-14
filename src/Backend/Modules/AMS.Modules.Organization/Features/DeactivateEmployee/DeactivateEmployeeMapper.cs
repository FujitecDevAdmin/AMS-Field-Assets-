namespace AMS.Modules.Organization.Features.DeactivateEmployee;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class DeactivateEmployeeMapper
{
    public static DeactivateEmployeeCommand ToCommand(DeactivateEmployeeRequest request, int employeeId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new DeactivateEmployeeCommand(
            employeeId,
            request.ETag);
    }
}
