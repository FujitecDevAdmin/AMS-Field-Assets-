using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.ServiceDesk.Features.SearchMyRequests;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchMyRequestsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/requests/mine", async (
                [AsParameters] SearchMyRequestsRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchMyRequestsMapper.ToQuery(request, currentUser.EmployeeId ?? 0);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.ServiceDesk.Raise)
            .WithName("SearchMyRequests")
            .Produces<SearchMyRequestsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
