using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Notifications.Features.SearchEmailSettings;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class SearchEmailSettingsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/email-settings", async (
                [AsParameters] SearchEmailSettingsRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = SearchEmailSettingsMapper.ToQuery(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Notifications.EmailSettingManage)
            .WithName("SearchEmailSettings")
            .Produces<SearchEmailSettingsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
