using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Assets.Features.GetAssetDashboard;

public static class GetAssetDashboardEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/dashboard", async (
                [AsParameters] GetAssetDashboardRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var result = await dispatcher.SendAsync(GetAssetDashboardMapper.ToQuery(request), ct);
                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Assets.View)
            .WithName("GetAssetDashboard")
            .Produces<GetAssetDashboardResponse>(StatusCodes.Status200OK);
    }
}
