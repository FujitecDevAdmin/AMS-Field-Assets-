using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Verification.Features.SearchAuditAssets;

public static class SearchAuditAssetsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/cycles/{auditId:int}/assets", async (
                int auditId,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var result = await dispatcher.SendAsync(
                    SearchAuditAssetsMapper.ToQuery(new SearchAuditAssetsRequest(auditId)), ct);
                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.FieldAssets.Manage)
            .WithName("SearchAuditAssets")
            .Produces<SearchAuditAssetsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
