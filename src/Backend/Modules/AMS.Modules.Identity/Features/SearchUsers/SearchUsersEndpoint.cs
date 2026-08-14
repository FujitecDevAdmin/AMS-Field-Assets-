using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Identity.Features.SearchUsers;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchUsersEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/users", async (
                [AsParameters] SearchUsersRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchUsersMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Identity.UserView)
            .WithName("SearchUsers")
            .Produces<SearchUsersResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
