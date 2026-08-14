using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.ServiceDesk.Features.UpdateSupportTeam;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class UpdateSupportTeamEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPut("/teams/{id:int}", async (
                int id,
                UpdateSupportTeamRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = UpdateSupportTeamMapper.ToCommand(request, id);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.ServiceDesk.TeamManage)
            .WithName("UpdateSupportTeam")
            .Produces<UpdateSupportTeamResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
