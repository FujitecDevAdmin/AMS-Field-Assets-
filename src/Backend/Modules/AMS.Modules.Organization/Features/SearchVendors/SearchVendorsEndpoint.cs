using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Organization.Features.SearchVendors;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchVendorsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/vendors", async (
                [AsParameters] SearchVendorsRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchVendorsMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Organization.View)
            .WithName("SearchVendors")
            .Produces<SearchVendorsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
