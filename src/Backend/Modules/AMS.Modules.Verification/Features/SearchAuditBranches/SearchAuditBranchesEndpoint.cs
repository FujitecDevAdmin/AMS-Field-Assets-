using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Verification.Features.SearchAuditBranches;

public static class SearchAuditBranchesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/audit-branches", async (
                [AsParameters] SearchAuditBranchesRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var result = await dispatcher.SendAsync(
                    SearchAuditBranchesMapper.ToQuery(request), ct);
                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.FieldAssets.Manage)
            .WithName("SearchAuditBranches")
            .Produces<SearchAuditBranchesResponse>(StatusCodes.Status200OK);
    }
}
