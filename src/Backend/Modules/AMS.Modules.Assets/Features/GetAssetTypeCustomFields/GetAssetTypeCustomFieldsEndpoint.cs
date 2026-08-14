using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Assets.Features.GetAssetTypeCustomFields;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class GetAssetTypeCustomFieldsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/types/{assetTypeId:int}/custom-fields", async (
                int assetTypeId,
                bool? includeInactive,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = GetAssetTypeCustomFieldsMapper.ToQuery(new GetAssetTypeCustomFieldsRequest(assetTypeId, includeInactive));
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Assets.View)
            .WithName("GetAssetTypeCustomFields")
            .Produces<GetAssetTypeCustomFieldsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            ;
    }
}
