namespace AMS.Modules.Organization.Features.UpdateEmployee;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateEmployeeMapper
{
    public static UpdateEmployeeCommand ToCommand(UpdateEmployeeRequest request, int employeeId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateEmployeeCommand(
            employeeId,
            request.EmployeeCode.Trim().ToUpperInvariant(),
            request.FullName.Trim(),
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            request.DepartmentId,
            request.LocationId,
            request.ReportingManagerId,
            request.ETag);
    }
}
