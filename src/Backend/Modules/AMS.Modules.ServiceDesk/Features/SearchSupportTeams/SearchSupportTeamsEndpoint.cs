using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.ServiceDesk.Features.SearchSupportTeams;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchSupportTeamsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/teams", async (
                [AsParameters] SearchSupportTeamsRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchSupportTeamsMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.ServiceDesk.View)
            .WithName("SearchSupportTeams")
            .Produces<SearchSupportTeamsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
