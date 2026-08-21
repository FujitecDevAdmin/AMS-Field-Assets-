namespace AMS.Modules.Verification.Features.SearchAuditAssets;

public static class SearchAuditAssetsMapper
{
    public static SearchAuditAssetsQuery ToQuery(SearchAuditAssetsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new SearchAuditAssetsQuery(request.AuditId);
    }
}
