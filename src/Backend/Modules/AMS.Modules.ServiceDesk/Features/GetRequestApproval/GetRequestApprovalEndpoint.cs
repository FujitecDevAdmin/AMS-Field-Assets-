using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.ServiceDesk.Features.GetRequestApproval;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class GetRequestApprovalEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/requests/{id:int}/approval", async (
                int id,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = GetRequestApprovalMapper.ToQuery(new GetRequestApprovalRequest(), id);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.ServiceDesk.View)
            .WithName("GetRequestApproval")
            .Produces<GetRequestApprovalResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            ;
    }
}
