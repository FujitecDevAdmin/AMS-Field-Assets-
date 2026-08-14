using AMS.SharedKernel.Messaging;
using Microsoft.AspNetCore.Mvc;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Discovery.Features.ReportInventory;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class ReportInventoryEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/inventory", async (
                [FromHeader(Name = "X-Ams-Agent-Key")] string? apiKey,
                ReportInventoryRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = ReportInventoryMapper.ToCommand(request, apiKey);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .AllowAnonymous()
            .WithName("ReportInventory")
            .Produces<ReportInventoryResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            ;
    }
}
