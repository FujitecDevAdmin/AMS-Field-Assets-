using AMS.Modules.Organization.PublicApi.Organization;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;

namespace AMS.Modules.Verification.Features.SearchAuditBranches;

public sealed class SearchAuditBranchesHandler(IBranchDirectory branches)
    : IRequestHandler<SearchAuditBranchesQuery, SearchAuditBranchesResponse>
{
    public async Task<Result<SearchAuditBranchesResponse>> HandleAsync(
        SearchAuditBranchesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rows = await branches.ListActiveAsync(ct);
        return new SearchAuditBranchesResponse(rows
            .Select(branch => new SearchAuditBranchesResponse.Row(
                branch.Id, branch.BranchCode, branch.BranchName))
            .ToArray());
    }
}
