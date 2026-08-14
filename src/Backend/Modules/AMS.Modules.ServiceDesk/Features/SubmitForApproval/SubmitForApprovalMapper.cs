namespace AMS.Modules.ServiceDesk.Features.SubmitForApproval;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SubmitForApprovalMapper
{
    public static SubmitForApprovalCommand ToCommand(SubmitForApprovalRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SubmitForApprovalCommand(
            id,
            request.ApprovalWorkflowId);
    }
}
