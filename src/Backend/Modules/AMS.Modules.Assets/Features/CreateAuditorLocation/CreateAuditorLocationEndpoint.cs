using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Assets.Features.CreateAuditorLocation;

public static class CreateAuditorLocationEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/auditor-locations", async (
                CreateAuditorLocationRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var result = await dispatcher.SendAsync(CreateAuditorLocationMapper.ToCommand(request), ct);
                return result.ToCreatedResult(response => $"/api/v1/assets/auditor-locations/{response.Id}");
            })
            .RequireCapability(Capabilities.Assets.FieldAssetManage)
            .WithName("CreateAuditorLocation")
            .Produces<CreateAuditorLocationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict);
    }
}
