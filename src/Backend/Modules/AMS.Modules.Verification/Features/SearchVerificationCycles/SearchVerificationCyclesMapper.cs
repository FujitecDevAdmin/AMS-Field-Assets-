namespace AMS.Modules.Verification.Features.SearchVerificationCycles;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchVerificationCyclesMapper
{
    public static SearchVerificationCyclesQuery ToQuery(SearchVerificationCyclesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchVerificationCyclesQuery(
            request.ActiveOnly ?? false);
    }
}
