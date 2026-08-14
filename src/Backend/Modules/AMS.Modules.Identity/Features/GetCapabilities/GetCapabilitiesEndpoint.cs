using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Identity.Features.GetCapabilities;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class GetCapabilitiesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/capabilities", async (
                [AsParameters] GetCapabilitiesRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = GetCapabilitiesMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Identity.UserView)
            .WithName("GetCapabilities")
            .Produces<GetCapabilitiesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
