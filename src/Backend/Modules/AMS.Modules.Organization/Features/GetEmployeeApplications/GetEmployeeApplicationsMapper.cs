namespace AMS.Modules.Organization.Features.GetEmployeeApplications;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetEmployeeApplicationsMapper
{
    public static GetEmployeeApplicationsQuery ToQuery(GetEmployeeApplicationsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetEmployeeApplicationsQuery(
            request.EmployeeId,
            request.IncludeRevoked ?? false);
    }
}
