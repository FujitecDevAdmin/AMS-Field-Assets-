using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Organization.Features.DeactivateEmployee;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class DeactivateEmployeeEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/employees/{employeeId:int}/deactivate", async (
                int employeeId,
                DeactivateEmployeeRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = DeactivateEmployeeMapper.ToCommand(request, employeeId);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Organization.EmployeeManage)
            .WithName("DeactivateEmployee")
            .Produces<DeactivateEmployeeResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status412PreconditionFailed)
            ;
    }
}
