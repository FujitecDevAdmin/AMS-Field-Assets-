using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Organization.Features.GetMyApplicationAccess;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class GetMyApplicationAccessEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/me/applications", async (
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = GetMyApplicationAccessMapper.ToQuery(new GetMyApplicationAccessRequest(), currentUser);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .WithName("GetMyApplicationAccess")
            .Produces<GetMyApplicationAccessResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()

            ;
    }
}
