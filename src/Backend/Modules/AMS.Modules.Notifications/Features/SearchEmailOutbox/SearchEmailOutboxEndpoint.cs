using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Notifications.Features.SearchEmailOutbox;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchEmailOutboxEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/outbox", async (
                [AsParameters] SearchEmailOutboxRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchEmailOutboxMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Notifications.OutboxManage)
            .WithName("SearchEmailOutbox")
            .Produces<SearchEmailOutboxResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
