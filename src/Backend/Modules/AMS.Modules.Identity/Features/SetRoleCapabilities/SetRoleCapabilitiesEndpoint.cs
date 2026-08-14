using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Identity.Features.SetRoleCapabilities;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SetRoleCapabilitiesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPut("/roles/{roleId:int}/capabilities", async (
                int roleId,
                SetRoleCapabilitiesRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SetRoleCapabilitiesMapper.ToCommand(request, roleId);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Identity.RoleManage)
            .WithName("SetRoleCapabilities")
            .Produces<SetRoleCapabilitiesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            ;
    }
}
