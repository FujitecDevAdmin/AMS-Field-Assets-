namespace AMS.Modules.Organization.Features.CreateEmployee;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateEmployeeMapper
{
    public static CreateEmployeeCommand ToCommand(CreateEmployeeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateEmployeeCommand(
            request.EmployeeCode.Trim().ToUpperInvariant(),
            request.FullName.Trim(),
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            request.DepartmentId,
            request.LocationId,
            request.ReportingManagerId);
    }
}
