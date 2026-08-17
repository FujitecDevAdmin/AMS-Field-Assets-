using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Assets.Features.ListAuditorLocations;

public static class ListAuditorLocationsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/auditor-locations", async (
                [AsParameters] ListAuditorLocationsRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var result = await dispatcher.SendAsync(ListAuditorLocationsMapper.ToQuery(request), ct);
                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Assets.View)
            .WithName("ListAuditorLocations")
            .Produces<ListAuditorLocationsResponse>(StatusCodes.Status200OK);
    }
}
