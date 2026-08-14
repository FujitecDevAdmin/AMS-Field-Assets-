using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.ServiceDesk.Features.SearchRequestQueue;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchRequestQueueEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/requests", async (
                [AsParameters] SearchRequestQueueRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchRequestQueueMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.ServiceDesk.View)
            .WithName("SearchRequestQueue")
            .Produces<SearchRequestQueueResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
