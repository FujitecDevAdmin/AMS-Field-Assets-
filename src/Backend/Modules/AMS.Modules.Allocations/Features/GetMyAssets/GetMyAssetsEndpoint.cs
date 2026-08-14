using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Allocations.Features.GetMyAssets;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class GetMyAssetsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/mine", async (
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = GetMyAssetsMapper.ToQuery(new GetMyAssetsRequest());
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .WithName("GetMyAssets")
            .Produces<GetMyAssetsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
