using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Organization.Features.SearchEmployees;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchEmployeesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/employees", async (
                [AsParameters] SearchEmployeesRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchEmployeesMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Organization.EmployeeView)
            .WithName("SearchEmployees")
            .Produces<SearchEmployeesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
