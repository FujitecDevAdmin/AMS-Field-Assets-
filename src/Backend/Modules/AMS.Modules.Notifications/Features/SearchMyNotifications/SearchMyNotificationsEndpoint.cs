using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Notifications.Features.SearchMyNotifications;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchMyNotificationsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/notifications/mine", async (
                [AsParameters] SearchMyNotificationsRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchMyNotificationsMapper.ToQuery(request, currentUser.Id);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .WithName("SearchMyNotifications")
            .Produces<SearchMyNotificationsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
