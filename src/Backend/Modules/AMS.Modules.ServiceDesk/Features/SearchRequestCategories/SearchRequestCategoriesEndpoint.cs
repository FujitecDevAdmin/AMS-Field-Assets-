using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.ServiceDesk.Features.SearchRequestCategories;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchRequestCategoriesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/categories", async (
                [AsParameters] SearchRequestCategoriesRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchRequestCategoriesMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.ServiceDesk.View)
            .WithName("SearchRequestCategories")
            .Produces<SearchRequestCategoriesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
