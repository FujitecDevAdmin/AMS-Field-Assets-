namespace AMS.Modules.Verification.Features.SearchVerifications;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchVerificationsMapper
{
    public static SearchVerificationsQuery ToQuery(SearchVerificationsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchVerificationsQuery(
            request.CycleId,
            request.LocationId,
            string.IsNullOrWhiteSpace(request.WorkingCondition) ? null : request.WorkingCondition.Trim(),
            request.ExceptionsOnly ?? false,
            request.MismatchesOnly ?? false,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
