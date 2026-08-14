using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Identity.Features.GetUserCapabilities;

/// <summary>Route, capability, typed results. No logic (docs/02 §6).</summary>
public static class GetUserCapabilitiesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/users/{userId:int}/capabilities", async (
                int userId,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var query = GetUserCapabilitiesMapper.ToQuery(new GetUserCapabilitiesRequest(userId));
                var result = await dispatcher.SendAsync(query, ct);

                // A query returns 200; only a command that created a row is 201.
                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Identity.UserView)
            .WithName("GetUserCapabilities")
            .Produces<GetUserCapabilitiesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);
    }
}
