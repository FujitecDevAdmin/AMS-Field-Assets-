using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.ServiceDesk.Features.CreateServiceTemplate;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class CreateServiceTemplateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/templates", async (
                CreateServiceTemplateRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = CreateServiceTemplateMapper.ToCommand(request);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToCreatedResult(response => $"/api/v1/service-desk/templates/{response.Id}");
            })
            .RequireCapability(Capabilities.ServiceDesk.TemplateManage)
            .WithName("CreateServiceTemplate")
            .Produces<CreateServiceTemplateResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
