using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Organization.Features.RevokeApplicationAccess;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class RevokeApplicationAccessEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/employees/{employeeId:int}/applications/{applicationId:int}/revoke", async (
                int employeeId,
                int applicationId,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = RevokeApplicationAccessMapper.ToCommand(new RevokeApplicationAccessRequest(), employeeId, applicationId);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Organization.ApplicationAccessManage)
            .WithName("RevokeApplicationAccess")
            .Produces<RevokeApplicationAccessResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            ;
    }
}
