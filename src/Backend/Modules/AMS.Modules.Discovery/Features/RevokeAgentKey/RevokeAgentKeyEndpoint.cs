using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Discovery.Features.RevokeAgentKey;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class RevokeAgentKeyEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/agent-keys/{id:int}/revocation", async (
                int id,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = RevokeAgentKeyMapper.ToCommand(new RevokeAgentKeyRequest(), id);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Discovery.AgentKeyManage)
            .WithName("RevokeAgentKey")
            .Produces<RevokeAgentKeyResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
