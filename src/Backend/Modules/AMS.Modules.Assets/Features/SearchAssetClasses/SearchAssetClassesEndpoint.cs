using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Assets.Features.SearchAssetClasses;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchAssetClassesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/classes", async (
                [AsParameters] SearchAssetClassesRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchAssetClassesMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Assets.View)
            .WithName("SearchAssetClasses")
            .Produces<SearchAssetClassesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
