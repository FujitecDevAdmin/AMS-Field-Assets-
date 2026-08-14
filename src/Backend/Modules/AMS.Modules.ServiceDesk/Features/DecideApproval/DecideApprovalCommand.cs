using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.DecideApproval;

/// <summary>
/// Approve or reject the level waiting on me. Catalogue: My Approvals.
/// </summary>
public sealed record DecideApprovalCommand(
    long ParticipantId,
    Guid ClientDecisionId,
    bool Approved,
    string? Remarks,
    string Source) : ICommand<DecideApprovalResponse>;
