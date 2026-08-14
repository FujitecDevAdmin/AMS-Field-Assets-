using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Allocations.Features.MapAssetToSite;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class MapAssetToSiteEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/customer-sites/{id:int}/assets", async (
                int id,
                MapAssetToSiteRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = MapAssetToSiteMapper.ToCommand(request, id);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToCreatedResult(response => $"/api/v1/allocations/customer-sites/{id}/assets/{response.Id}");
            })
            .RequireCapability(Capabilities.Allocations.CustomerSiteManage)
            .WithName("MapAssetToSite")
            .Produces<MapAssetToSiteResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
