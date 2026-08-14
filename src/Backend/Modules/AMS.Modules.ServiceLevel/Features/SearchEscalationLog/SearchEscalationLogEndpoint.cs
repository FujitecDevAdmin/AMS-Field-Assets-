using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.ServiceLevel.Features.SearchEscalationLog;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchEscalationLogEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/escalation-log", async (
                [AsParameters] SearchEscalationLogRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchEscalationLogMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.ServiceLevel.SlaManage)
            .WithName("SearchEscalationLog")
            .Produces<SearchEscalationLogResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
