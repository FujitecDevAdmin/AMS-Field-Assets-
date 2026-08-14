namespace AMS.Modules.Organization.Features.GetMyApplicationAccess;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetMyApplicationAccessMapper
{
    public static GetMyApplicationAccessQuery ToQuery(GetMyApplicationAccessRequest request, AMS.SharedKernel.Abstractions.ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetMyApplicationAccessQuery(
            currentUser.EmployeeId);
    }
}
