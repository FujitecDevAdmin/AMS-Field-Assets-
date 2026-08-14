namespace AMS.Modules.ServiceDesk.Features.GetRequestApproval;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetRequestApprovalMapper
{
    public static GetRequestApprovalQuery ToQuery(GetRequestApprovalRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetRequestApprovalQuery(
            id);
    }
}
