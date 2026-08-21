using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Verification.Features.SearchVerificationCycles;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchVerificationCyclesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/cycles", async (
                [AsParameters] SearchVerificationCyclesRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchVerificationCyclesMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.FieldAssets.Manage)
            .WithName("SearchVerificationCycles")
            .Produces<SearchVerificationCyclesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
