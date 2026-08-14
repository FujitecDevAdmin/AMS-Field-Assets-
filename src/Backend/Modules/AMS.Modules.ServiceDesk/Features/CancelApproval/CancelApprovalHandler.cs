using AMS.Modules.ServiceDesk.Approvals;
using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.CancelApproval;

/// <summary>
/// Call off an approval run. Catalogue: the approval panel on Request Detail.
/// </summary>
/// <remarks>
/// The run is not deleted — nothing in this block ever is (R2-12). It is
/// marked Cancelled, with who and why, and every level that had not finished is
/// closed out with it. A run that simply vanished would leave the request
/// looking as though it had never been submitted, which is precisely the
/// question an audit asks.
/// </remarks>
public sealed class CancelApprovalHandler(
    ServiceDeskDbContext db,
    ApprovalNotifications notifications,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<CancelApprovalCommand, CancelApprovalResponse>
{
    public async Task<Result<CancelApprovalResponse>> HandleAsync(
        CancelApprovalCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var instance = await db.RequestApprovalInstances
            .Where(i => i.ServiceRequestId == request.Id
                && i.Status == ApprovalInstanceStatus.Pending)
            .SingleOrDefaultAsync(ct);

        if (instance is null)
        {
            return Error.NotFound("RequestApproval", request.Id);
        }

        var now = clock.UtcNow;

        instance.Status = ApprovalInstanceStatus.Cancelled;
        instance.CancelledOnUtc = now;
        instance.CancelledByUserId = currentUser.Id;
        instance.CancellationReason = request.Reason;
        instance.CompletedOnUtc = now;
        instance.CurrentStageNumber = null;
        instance.ModifiedOnUtc = now;
        instance.ModifiedBy = currentUser.Username;

        var steps = await db.RequestApprovalSteps
            .Where(s => s.RequestApprovalInstanceId == instance.Id
                && (s.Status == ApprovalStepStatus.Waiting
                    || s.Status == ApprovalStepStatus.Pending))
            .ToListAsync(ct);

        var stepIds = steps.ConvertAll(s => s.Id);

        foreach (var step in steps)
        {
            step.Status = ApprovalStepStatus.Cancelled;
            step.CompletedOnUtc = now;
            step.OutcomeRemarks = "The approval was cancelled.";
            step.ModifiedOnUtc = now;
            step.ModifiedBy = currentUser.Username;
        }

        // Otherwise these sit in somebody's My Approvals asking for a decision
        // that can no longer change anything.
        var waiting = await db.RequestApprovalParticipants
            .Where(p => stepIds.Contains(p.RequestApprovalStepId)
                && (p.ParticipantStatus == ParticipantStatus.Waiting
                    || p.ParticipantStatus == ParticipantStatus.Pending))
            .ToListAsync(ct);

        foreach (var participant in waiting)
        {
            participant.ParticipantStatus = ParticipantStatus.Cancelled;
        }

        db.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = instance.ServiceRequestId,
            EntryKind = HistoryEntryKind.Automation,
            EntryText = "Approval cancelled.",
            Body = request.Reason,
            OccurredOnUtc = now,
            PerformedBy = currentUser.Username,
        });

        await db.SaveChangesAsync(ct);

        var ticket = await db.ServiceRequests.SingleAsync(
            r => r.Id == instance.ServiceRequestId, ct);

        await notifications.AnnounceAsync(
            instance, ticket, ApprovalNotificationType.RequestCancelled, ct);

        // Everybody who was waiting on it, so it stops sitting in their list
        // asking for a decision that can no longer change anything.
        var waitingUsers = waiting
            .Where(p => p.ApproverUserId is not null)
            .Select(p => p.ApproverUserId!.Value)
            .ToList();

        if (waitingUsers.Count > 0)
        {
            await notifications.NotifyCancelledAsync(waitingUsers, ticket, ct);
        }

        return new CancelApprovalResponse(
            instance.Id, instance.ServiceRequestId, instance.Status, now);
    }
}
