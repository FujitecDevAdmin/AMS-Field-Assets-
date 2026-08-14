using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Assets.Features.DefineCustomField;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class DefineCustomFieldEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/types/{assetTypeId:int}/custom-fields", async (
                int assetTypeId,
                DefineCustomFieldRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = DefineCustomFieldMapper.ToCommand(request, assetTypeId);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToCreatedResult(response => $"/api/v1/assets/types/{assetTypeId}/custom-fields/{response.Id}");
            })
            .RequireCapability(Capabilities.Assets.TaxonomyManage)
            .WithName("DefineCustomField")
            .Produces<DefineCustomFieldResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
