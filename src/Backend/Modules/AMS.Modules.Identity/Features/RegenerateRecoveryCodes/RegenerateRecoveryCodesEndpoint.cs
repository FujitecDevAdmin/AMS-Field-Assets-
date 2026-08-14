using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Identity.Features.RegenerateRecoveryCodes;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class RegenerateRecoveryCodesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/me/mfa/recovery-codes", async (
                RegenerateRecoveryCodesRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = RegenerateRecoveryCodesMapper.ToCommand(request, currentUser);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .WithName("RegenerateRecoveryCodes")
            .Produces<RegenerateRecoveryCodesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status403Forbidden)
            ;
    }
}
