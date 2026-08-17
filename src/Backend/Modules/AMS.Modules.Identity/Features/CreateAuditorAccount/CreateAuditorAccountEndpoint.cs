using AMS.Modules.Identity.PublicApi;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Identity.Features.CreateAuditorAccount;

public static class CreateAuditorAccountEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapPost("/auditors", async (
                CreateAuditorAccountRequest request,
                IDispatcher dispatcher,
                IPasswordHasher hasher,
                CancellationToken ct) =>
            {
                var result = await dispatcher.SendAsync(
                    CreateAuditorAccountMapper.ToCommand(request, hasher.Hash(request.Password)), ct);
                return result.ToCreatedResult(response => $"/api/v1/identity/users/{response.Id}");
            })
            .RequireCapability(Capabilities.Identity.FieldAssetManage)
            .WithName("CreateAuditorAccount")
            .Produces<CreateAuditorAccountResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict);
    }
}
