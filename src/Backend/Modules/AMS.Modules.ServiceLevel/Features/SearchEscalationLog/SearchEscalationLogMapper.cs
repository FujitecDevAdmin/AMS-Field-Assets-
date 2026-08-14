namespace AMS.Modules.ServiceLevel.Features.SearchEscalationLog;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchEscalationLogMapper
{
    public static SearchEscalationLogQuery ToQuery(SearchEscalationLogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchEscalationLogQuery(
            request.ServiceRequestId,
            string.IsNullOrWhiteSpace(request.Outcome) ? null : request.Outcome.Trim(),
            request.Take ?? 100);
    }
}
