namespace AMS.Modules.Verification.Features.SearchMyAudits;

public static class SearchMyAuditsMapper
{
    public static SearchMyAuditsQuery ToQuery(SearchMyAuditsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new SearchMyAuditsQuery();
    }
}
