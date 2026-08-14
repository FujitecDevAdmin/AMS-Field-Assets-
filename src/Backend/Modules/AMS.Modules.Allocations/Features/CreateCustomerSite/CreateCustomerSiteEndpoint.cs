using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Allocations.Features.CreateCustomerSite;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class CreateCustomerSiteEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/customer-sites", async (
                CreateCustomerSiteRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = CreateCustomerSiteMapper.ToCommand(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToCreatedResult(response => $"/api/v1/allocations/customer-sites/{response.Id}");
            })
            .RequireCapability(Capabilities.Allocations.CustomerSiteManage)
            .WithName("CreateCustomerSite")
            .Produces<CreateCustomerSiteResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
