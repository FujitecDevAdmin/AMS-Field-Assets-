using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Assets.Features.UpdateImportedAssetDetails;

public static class UpdateImportedAssetDetailsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPut("/{id:int}/imported-details", async (
                int id,
                UpdateImportedAssetDetailsRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var result = await dispatcher.SendAsync(
                    new UpdateImportedAssetDetailsCommand(id, request.Fields),
                    ct);
                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Assets.FieldAssetManage)
            .WithName("UpdateImportedAssetDetails")
            .Produces<UpdateImportedAssetDetailsResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
