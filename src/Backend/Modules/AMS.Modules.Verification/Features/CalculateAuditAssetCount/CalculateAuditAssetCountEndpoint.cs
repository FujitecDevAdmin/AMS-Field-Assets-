using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Verification.Features.CalculateAuditAssetCount;

public static class CalculateAuditAssetCountEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/audit-asset-count", async (
                CalculateAuditAssetCountRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var result = await dispatcher.SendAsync(
                    CalculateAuditAssetCountMapper.ToQuery(request), ct);
                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.FieldAssets.Manage)
            .WithName("CalculateAuditAssetCount")
            .Produces<CalculateAuditAssetCountResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}
