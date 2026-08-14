using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Assets.Features.DeleteAsset;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class DeleteAssetEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        // The reason is a query parameter, not a body. Minimal API refuses to
        // infer a body on DELETE - the host returns 500 for every route in the
        // application, not just this one, because endpoint building fails as a
        // whole. Only booting the host finds it; a handler test cannot.
        group.MapDelete("/{id:int}", async (
                int id,
                string? reason,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = DeleteAssetMapper.ToCommand(new DeleteAssetRequest(reason), id);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Assets.Manage)
            .WithName("DeleteAsset")
            .Produces<DeleteAssetResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
