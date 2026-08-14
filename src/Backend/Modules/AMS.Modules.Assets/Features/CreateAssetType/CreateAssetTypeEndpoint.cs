using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Assets.Features.CreateAssetType;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class CreateAssetTypeEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/types", async (
                CreateAssetTypeRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = CreateAssetTypeMapper.ToCommand(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToCreatedResult(response => $"/api/v1/assets/types/{response.Id}");
            })
            .RequireCapability(Capabilities.Assets.TaxonomyManage)
            .WithName("CreateAssetType")
            .Produces<CreateAssetTypeResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
