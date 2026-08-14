using AMS.Modules.Identity.PublicApi;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Identity.Features.CreateUser;

/// <summary>
/// Route, capability, typed results. Zero logic — if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class CreateUserEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/users", async (
                CreateUserRequest request,
                IDispatcher dispatcher,
                IPasswordHasher hasher,
                CancellationToken ct) =>
            {
                var command = CreateUserMapper.ToCommand(request, hasher.Hash(request.Password));
                var result = await dispatcher.SendAsync(command, ct);

                // 201 because the command created a row; the slice decides
                // this, ToHttpResult does not guess (02 §3).
                return result.ToCreatedResult(response => $"/api/v1/identity/users/{response.Id}");
            })
            .RequireCapability(Capabilities.Identity.UserManage)
            .WithName("CreateUser")
            .Produces<CreateUserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict);
    }
}
