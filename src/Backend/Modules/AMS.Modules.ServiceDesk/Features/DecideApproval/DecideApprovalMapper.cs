using AMS.Modules.ServiceDesk.Domain;

namespace AMS.Modules.ServiceDesk.Features.DecideApproval;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class DecideApprovalMapper
{
    public static DecideApprovalCommand ToCommand(DecideApprovalRequest request, long participantId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new DecideApprovalCommand(
            participantId,
            request.ClientDecisionId ?? Guid.NewGuid(),
            request.Approved,
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
            string.IsNullOrWhiteSpace(request.Source) ? DecisionSource.Application : request.Source.Trim());
    }
}
