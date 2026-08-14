using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Discovery.Features.SearchInstalledSoftware;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchInstalledSoftwareEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/software", async (
                [AsParameters] SearchInstalledSoftwareRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchInstalledSoftwareMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Discovery.View)
            .WithName("SearchInstalledSoftware")
            .Produces<SearchInstalledSoftwareResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
