using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Discovery.Features.ResolveDiscoveredDevice;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class ResolveDiscoveredDeviceEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/devices/{id:int}/resolution", async (
                int id,
                ResolveDiscoveredDeviceRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = ResolveDiscoveredDeviceMapper.ToCommand(request, id);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Discovery.Manage)
            .WithName("ResolveDiscoveredDevice")
            .Produces<ResolveDiscoveredDeviceResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
