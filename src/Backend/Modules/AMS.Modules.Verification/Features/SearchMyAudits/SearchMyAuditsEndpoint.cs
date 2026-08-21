using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Verification.Features.SearchMyAudits;

public static class SearchMyAuditsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/my-audits", async (
                [AsParameters] SearchMyAuditsRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var result = await dispatcher.SendAsync(SearchMyAuditsMapper.ToQuery(request), ct);
                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Verification.Run)
            .WithName("SearchMyAudits")
            .Produces<SearchMyAuditsResponse>(StatusCodes.Status200OK);
    }
}
