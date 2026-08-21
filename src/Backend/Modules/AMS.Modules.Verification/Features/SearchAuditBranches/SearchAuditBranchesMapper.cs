namespace AMS.Modules.Verification.Features.SearchAuditBranches;

public static class SearchAuditBranchesMapper
{
    public static SearchAuditBranchesQuery ToQuery(SearchAuditBranchesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new SearchAuditBranchesQuery();
    }
}
