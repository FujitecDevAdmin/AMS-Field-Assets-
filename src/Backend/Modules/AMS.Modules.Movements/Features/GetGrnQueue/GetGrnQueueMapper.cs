namespace AMS.Modules.Movements.Features.GetGrnQueue;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetGrnQueueMapper
{
    public static GetGrnQueueQuery ToQuery(GetGrnQueueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetGrnQueueQuery(
            request.ToLocationId,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
