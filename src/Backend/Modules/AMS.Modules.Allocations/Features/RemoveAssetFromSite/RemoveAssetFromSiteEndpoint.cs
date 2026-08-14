using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Allocations.Features.RemoveAssetFromSite;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class RemoveAssetFromSiteEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapDelete("/customer-sites/assets/{id:int}", async (
                int id,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = RemoveAssetFromSiteMapper.ToCommand(new RemoveAssetFromSiteRequest(), id);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Allocations.CustomerSiteManage)
            .WithName("RemoveAssetFromSite")
            .Produces<RemoveAssetFromSiteResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
