using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.ServiceDesk.Features.PublishApprovalWorkflow;

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class PublishApprovalWorkflowEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/approval-workflows/{id:int}/publication", async (
                int id,
                PublishApprovalWorkflowRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var message = PublishApprovalWorkflowMapper.ToCommand(request, id);
                var result = await dispatcher.SendAsync(message, ct);

                return result.ToHttpResult();
            })
            .RequireCapability(Capabilities.ServiceDesk.WorkflowManage)
            .WithName("PublishApprovalWorkflow")
            .Produces<PublishApprovalWorkflowResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            ;
    }
}
