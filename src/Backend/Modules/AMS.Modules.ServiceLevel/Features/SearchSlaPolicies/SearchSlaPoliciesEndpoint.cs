using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.ServiceLevel.Features.SearchSlaPolicies;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchSlaPoliciesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/sla-policies", async (
                [AsParameters] SearchSlaPoliciesRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchSlaPoliciesMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.ServiceLevel.SlaManage)
            .WithName("SearchSlaPolicies")
            .Produces<SearchSlaPoliciesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
