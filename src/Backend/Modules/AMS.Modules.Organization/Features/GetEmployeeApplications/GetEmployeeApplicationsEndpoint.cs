using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Organization.Features.GetEmployeeApplications;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class GetEmployeeApplicationsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/employees/{employeeId:int}/applications", async (
                int employeeId,
                bool? includeRevoked,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = GetEmployeeApplicationsMapper.ToQuery(new GetEmployeeApplicationsRequest(employeeId, includeRevoked));
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Organization.EmployeeView)
            .WithName("GetEmployeeApplications")
            .Produces<GetEmployeeApplicationsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            ;
    }
}
