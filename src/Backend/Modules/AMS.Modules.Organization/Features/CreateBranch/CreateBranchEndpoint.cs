using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Organization.Features.CreateBranch;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class CreateBranchEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/branches", async (
                CreateBranchRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = CreateBranchMapper.ToCommand(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToCreatedResult(response => $"/api/v1/organization/branches/{response.Id}");
            })
            .RequireCapability(Capabilities.Organization.Manage)
            .WithName("CreateBranch")
            .Produces<CreateBranchResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
