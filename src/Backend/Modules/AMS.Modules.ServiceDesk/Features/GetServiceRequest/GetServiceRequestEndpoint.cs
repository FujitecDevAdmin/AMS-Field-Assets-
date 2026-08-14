using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.ServiceDesk.Features.GetServiceRequest;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class GetServiceRequestEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/requests/{id:int}", async (
                int id,
                [AsParameters] GetServiceRequestRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = GetServiceRequestMapper.ToQuery(request, id);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.ServiceDesk.View)
            .WithName("GetServiceRequest")
            .Produces<GetServiceRequestResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            ;
    }
}
