namespace AMS.Modules.Organization.Features.GetEmployee;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetEmployeeMapper
{
    public static GetEmployeeQuery ToQuery(GetEmployeeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetEmployeeQuery(
            request.EmployeeId);
    }
}
