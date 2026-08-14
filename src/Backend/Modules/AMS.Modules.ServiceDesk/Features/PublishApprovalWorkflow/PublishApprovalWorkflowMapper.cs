namespace AMS.Modules.ServiceDesk.Features.PublishApprovalWorkflow;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class PublishApprovalWorkflowMapper
{
    public static PublishApprovalWorkflowCommand ToCommand(PublishApprovalWorkflowRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new PublishApprovalWorkflowCommand(
            id,
            request.IsPublished ?? true,
            request.IsActive ?? true,
            request.EffectiveFromUtc,
            request.EffectiveToUtc);
    }
}
