namespace AMS.Modules.ServiceDesk.Features.SearchMyApprovals;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchMyApprovalsMapper
{
    public static SearchMyApprovalsQuery ToQuery(SearchMyApprovalsRequest request, int userId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchMyApprovalsQuery(
            userId,
            request.PendingOnly ?? true,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
