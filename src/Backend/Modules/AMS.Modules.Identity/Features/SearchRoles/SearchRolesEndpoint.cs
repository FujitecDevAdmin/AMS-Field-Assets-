using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Identity.Features.SearchRoles;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchRolesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/roles", async (
                [AsParameters] SearchRolesRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchRolesMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Identity.UserView)
            .WithName("SearchRoles")
            .Produces<SearchRolesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
