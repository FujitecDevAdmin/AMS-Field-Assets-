namespace AMS.Modules.ServiceDesk.Features.SearchRequestQueue;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchRequestQueueMapper
{
    public static SearchRequestQueueQuery ToQuery(SearchRequestQueueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchRequestQueueQuery(
            string.IsNullOrWhiteSpace(request.RequestKind) ? null : request.RequestKind.Trim(),
            request.RequestStatusId,
            string.IsNullOrWhiteSpace(request.Priority) ? null : request.Priority.Trim(),
            request.AssignedToUserId,
            request.AssignedTeamId,
            request.LocationId,
            request.Unassigned ?? false,
            request.OverdueOnly ?? false,
            request.OpenOnly ?? true,
            string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
