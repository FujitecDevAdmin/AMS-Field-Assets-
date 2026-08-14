using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Assets.Features.CreateChartOfAccount;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class CreateChartOfAccountEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/chart-of-accounts", async (
                CreateChartOfAccountRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = CreateChartOfAccountMapper.ToCommand(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToCreatedResult(response => $"/api/v1/assets/chart-of-accounts/{response.Id}");
            })
            .RequireCapability(Capabilities.Assets.TaxonomyManage)
            .WithName("CreateChartOfAccount")
            .Produces<CreateChartOfAccountResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
