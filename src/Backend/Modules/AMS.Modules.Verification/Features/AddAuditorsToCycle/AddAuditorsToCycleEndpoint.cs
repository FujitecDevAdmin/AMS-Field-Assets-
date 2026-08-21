using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Verification.Features.AddAuditorsToCycle;

public static class AddAuditorsToCycleEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/cycles/{cycleId:int}/auditors", async (
                int cycleId,
                AddAuditorsToCycleRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var command = new AddAuditorsToCycleCommand(
                    cycleId,
                    request.AuditorUserIds.Distinct().ToArray());
                var result = await dispatcher.SendAsync(command, ct);
                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.FieldAssets.Manage)
            .WithName("AddAuditorsToCycle")
            .Produces<AddAuditorsToCycleResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }
}
