using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Transfers.Features.SearchTransferRequests;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchTransferRequestsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("", async (
                [AsParameters] SearchTransferRequestsRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchTransferRequestsMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Transfers.View)
            .WithName("SearchTransferRequests")
            .Produces<SearchTransferRequestsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
