using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Discovery.Features.SetSoftwareCatalogEntry;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SetSoftwareCatalogEntryEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPut("/software-catalog", async (
                SetSoftwareCatalogEntryRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SetSoftwareCatalogEntryMapper.ToCommand(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Discovery.SoftwareCatalogManage)
            .WithName("SetSoftwareCatalogEntry")
            .Produces<SetSoftwareCatalogEntryResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
