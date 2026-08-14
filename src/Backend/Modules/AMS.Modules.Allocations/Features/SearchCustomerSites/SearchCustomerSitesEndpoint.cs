using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Allocations.Features.SearchCustomerSites;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchCustomerSitesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/customer-sites", async (
                [AsParameters] SearchCustomerSitesRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchCustomerSitesMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Allocations.View)
            .WithName("SearchCustomerSites")
            .Produces<SearchCustomerSitesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
