using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.ServiceLevel.Features.GetLocationCalendar;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class GetLocationCalendarEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/locations/{locationId:int}/calendar", async (
                int locationId,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = GetLocationCalendarMapper.ToQuery(new GetLocationCalendarRequest(), locationId);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.ServiceLevel.CalendarManage)
            .WithName("GetLocationCalendar")
            .Produces<GetLocationCalendarResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            ;
    }
}
