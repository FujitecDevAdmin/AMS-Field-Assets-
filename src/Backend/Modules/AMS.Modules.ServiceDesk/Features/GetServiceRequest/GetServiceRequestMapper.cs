namespace AMS.Modules.ServiceDesk.Features.GetServiceRequest;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetServiceRequestMapper
{
    public static GetServiceRequestQuery ToQuery(GetServiceRequestRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetServiceRequestQuery(
            id,
            request.IncludeInternal ?? false);
    }
}
