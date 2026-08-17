using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Identity.Features.ListAuditorAccounts;

public static class ListAuditorAccountsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/auditors", async ([AsParameters] ListAuditorAccountsRequest request,
                IDispatcher dispatcher, CancellationToken ct) =>
            {
                var result = await dispatcher.SendAsync(ListAuditorAccountsMapper.ToQuery(request), ct);
                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Identity.FieldAssetManage)
            .WithName("ListAuditorAccounts")
            .Produces<ListAuditorAccountsResponse>(StatusCodes.Status200OK);
    }
}
