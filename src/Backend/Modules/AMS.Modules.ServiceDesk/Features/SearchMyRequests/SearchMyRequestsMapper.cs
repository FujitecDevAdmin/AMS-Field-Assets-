namespace AMS.Modules.ServiceDesk.Features.SearchMyRequests;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchMyRequestsMapper
{
    public static SearchMyRequestsQuery ToQuery(SearchMyRequestsRequest request, int employeeId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchMyRequestsQuery(
            employeeId,
            request.OpenOnly ?? false,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
