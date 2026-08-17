using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Organization.Features.UpdateBranch;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class UpdateBranchEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPut("/branches/{id:int}", async (
                int id,
                UpdateBranchRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = UpdateBranchMapper.ToCommand(request, id);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.Organization.Manage)
            .WithName("UpdateBranch")
            .Produces<UpdateBranchResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
