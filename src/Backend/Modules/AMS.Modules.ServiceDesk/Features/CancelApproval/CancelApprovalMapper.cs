namespace AMS.Modules.ServiceDesk.Features.CancelApproval;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CancelApprovalMapper
{
    public static CancelApprovalCommand ToCommand(CancelApprovalRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CancelApprovalCommand(
            id,
            request.Reason.Trim());
    }
}
