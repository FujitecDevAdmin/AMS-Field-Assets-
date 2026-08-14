using AMS.Modules.ServiceDesk.Approvals;
using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.DecideApproval;

/// <summary>
/// Approve or reject the level waiting on me. Catalogue: My Approvals.
/// </summary>
/// <remarks>
/// <para>
/// The decision row is append-only evidence: written once, never updated,
/// never deleted (R2-12), and carrying a NO ACTION foreign key so the run it
/// belongs to cannot be removed either. Everything else this handler does —
/// settling the level, moving the run on — is bookkeeping derived from it.
/// </para>
/// <para>
/// <c>ClientDecisionId</c> makes a retry safe. An approval clicked in an
/// e-mail on a bad connection, or a request the browser resent, must not
/// become two decisions; the second call finds the first and returns the same
/// answer, with <c>WasAlreadyDecided</c> set so the caller can tell.
/// </para>
/// </remarks>
public sealed class DecideApprovalHandler(
    ServiceDeskDbContext db,
    ApproverResolver resolver,
    ApprovalNotifications notifications,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<DecideApprovalCommand, DecideApprovalResponse>
{
    public async Task<Result<DecideApprovalResponse>> HandleAsync(
        DecideApprovalCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!DecisionSource.Allowed.Contains(request.Source, StringComparer.Ordinal))
        {
            return Error.Validation(
                "RequestApprovalDecision.UnknownSource",
                $"Source must be one of {string.Join(", ", DecisionSource.Allowed)}.");
        }

        var participant = await db.RequestApprovalParticipants
            .SingleOrDefaultAsync(p => p.Id == request.ParticipantId, ct);

        if (participant is null)
        {
            return Error.NotFound("RequestApprovalParticipant", request.ParticipantId);
        }

        // The replay check comes before every other rule. A retry of a decision
        // that already closed its level would otherwise be refused for
        // "the level is finished" - technically true, and useless to a client
        // that is asking whether its own earlier call got through.
        var existing = await db.RequestApprovalDecisions
            .SingleOrDefaultAsync(d => d.ClientDecisionId == request.ClientDecisionId, ct);

        if (existing is not null)
        {
            return await ReplayAsync(existing, ct);
        }

        var step = await db.RequestApprovalSteps
            .SingleAsync(s => s.Id == participant.RequestApprovalStepId, ct);

        var instance = await db.RequestApprovalInstances
            .SingleAsync(i => i.Id == step.RequestApprovalInstanceId, ct);

        if (instance.Status != ApprovalInstanceStatus.Pending)
        {
            return Error.Conflict(
                "RequestApproval.Finished",
                $"This approval was already {instance.Status.ToLowerInvariant()}.");
        }

        if (step.Status != ApprovalStepStatus.Pending)
        {
            return Error.Conflict(
                "RequestApprovalStep.NotActive",
                $"{step.StageNameSnapshot} is not the level currently waiting.");
        }

        if (participant.ParticipantStatus != ParticipantStatus.Pending)
        {
            return Error.Conflict(
                "RequestApprovalParticipant.AlreadyDecided",
                $"You have already {participant.ParticipantStatus.ToLowerInvariant()} this.");
        }

        // Whose decision this is. The signed-in user must be the participant:
        // an approval recorded against somebody who did not make it is worse
        // than no approval at all.
        if (participant.ApproverUserId is { } approverId && approverId != currentUser.Id)
        {
            return Error.Forbidden(
                "RequestApprovalParticipant.NotYours",
                "That approval is waiting on somebody else.");
        }

        var now = clock.UtcNow;
        var outcome = request.Approved ? ApprovalDecision.Approved : ApprovalDecision.Rejected;

        db.RequestApprovalDecisions.Add(new RequestApprovalDecision
        {
            RequestApprovalParticipantId = participant.Id,
            ClientDecisionId = request.ClientDecisionId,
            Decision = outcome,
            Remarks = request.Remarks,
            ActedByUserId = currentUser.Id,
            // The address as it was snapshotted, not the account's address
            // today. This row has to still make sense when the account is gone.
            ActedByEmailSnapshot = participant.ApproverEmailSnapshot,
            Source = request.Source,
            DecidedOnUtc = now,
        });

        participant.ParticipantStatus = request.Approved
            ? ParticipantStatus.Approved
            : ParticipantStatus.Rejected;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        var siblings = await db.RequestApprovalParticipants
            .Where(p => p.RequestApprovalStepId == step.Id)
            .ToListAsync(ct);

        var settled = ApprovalRun.Settle(step.ApprovalModeSnapshot, siblings);

        if (settled is not null)
        {
            await SettleStepAsync(step, instance, siblings, settled, now, ct);
        }

        await db.SaveChangesAsync(ct);

        if (settled is not null)
        {
            // After the save, so a notification is never sent for a decision
            // that did not stick. The outbox insert is in the same transaction,
            // so nothing escapes if the commit fails either.
            await AnnounceAsync(instance, step, settled, ct);
        }

        return new DecideApprovalResponse(
            participant.Id, outcome, step.Status, instance.Status,
            instance.CurrentStageNumber, WasAlreadyDecided: false);
    }

    /// <summary>Tells whoever needs to know what the decision did.</summary>
    private async Task AnnounceAsync(
        RequestApprovalInstance instance,
        RequestApprovalStep step,
        string settled,
        CancellationToken ct)
    {
        var ticket = await db.ServiceRequests.SingleAsync(
            r => r.Id == instance.ServiceRequestId, ct);

        // Whoever was asked and never answered, first and in every case. On a
        // one-level route the run finishes in the same breath, and an earlier
        // shape returned before telling them - leaving an approval sitting in
        // their list asking for a decision that could no longer change
        // anything.
        if (settled == ApprovalStepStatus.Approved)
        {
            await notifications.AnnounceStepApprovedAsync(instance, step, ticket, ct);
        }

        if (instance.Status != ApprovalInstanceStatus.Pending)
        {
            await notifications.AnnounceAsync(
                instance,
                ticket,
                instance.Status == ApprovalInstanceStatus.Approved
                    ? ApprovalNotificationType.RequestApproved
                    : ApprovalNotificationType.RequestRejected,
                ct);

            return;
        }

        if (settled == ApprovalStepStatus.Approved)
        {
            // The run moved on, so the next level's approvers are waiting and
            // do not know it yet.
            var next = await db.RequestApprovalSteps.SingleAsync(
                s => s.RequestApprovalInstanceId == instance.Id
                    && s.Status == ApprovalStepStatus.Pending, ct);

            await notifications.AskAsync(instance, next, ticket, ct);
        }
    }

    /// <summary>Closes a level and does to the run whatever that implies.</summary>
    private async Task SettleStepAsync(
        RequestApprovalStep step,
        RequestApprovalInstance instance,
        IReadOnlyList<RequestApprovalParticipant> participants,
        string settled,
        DateTime now,
        CancellationToken ct)
    {
        step.Status = settled;
        step.CompletedOnUtc = now;
        step.ModifiedOnUtc = now;
        step.ModifiedBy = currentUser.Username;

        // Anybody who never got to answer is closed out with the level. Left
        // Pending they would sit in somebody's My Approvals for ever, asking
        // for a decision that can no longer change anything.
        foreach (var waiting in participants.Where(p =>
                     p.ParticipantStatus is ParticipantStatus.Waiting or ParticipantStatus.Pending))
        {
            waiting.ParticipantStatus = ParticipantStatus.Cancelled;
        }

        if (settled == ApprovalStepStatus.Rejected)
        {
            step.OutcomeRemarks = "Rejected at this level.";
            await FinishAsync(instance, ApprovalInstanceStatus.Rejected, now, ct);
            return;
        }

        var next = await db.RequestApprovalSteps
            .Where(s => s.RequestApprovalInstanceId == instance.Id
                && s.StageNumber > step.StageNumber
                && s.Status == ApprovalStepStatus.Waiting)
            .OrderBy(s => s.StageNumber)
            .FirstOrDefaultAsync(ct);

        if (next is null)
        {
            await FinishAsync(instance, ApprovalInstanceStatus.Approved, now, ct);
            return;
        }

        var stage = await db.ApprovalWorkflowStages
            .SingleAsync(s => s.Id == next.ApprovalWorkflowStageId, ct);

        ApprovalRun.Activate(next, stage, now);
        instance.CurrentStageNumber = next.StageNumber;
        instance.ModifiedOnUtc = now;
        instance.ModifiedBy = currentUser.Username;

        // The next level's approvers are resolved NOW, not at submission: a
        // fortnight into a long approval, "the requester's manager" should be
        // whoever that is today.
        var ticket = await db.ServiceRequests.SingleAsync(r => r.Id == instance.ServiceRequestId, ct);

        var rules = await db.ApprovalStageApproverRules
            .Where(r => r.ApprovalWorkflowStageId == stage.Id)
            .OrderBy(r => r.Id)
            .ToListAsync(ct);

        var resolved = await resolver.ResolveAsync(
            rules,
            new ApprovalContext(
                ticket.RequestedByEmployeeId, ticket.OnBehalfOfEmployeeId, ticket.LocationId),
            ct);

        foreach (var (rule, approver) in resolved)
        {
            db.RequestApprovalParticipants.Add(new RequestApprovalParticipant
            {
                RequestApprovalStepId = next.Id,
                ApproverRuleId = rule.Id,
                ApproverUserId = approver.UserId,
                ApproverEmployeeId = approver.EmployeeId,
                ApproverNameSnapshot = approver.Name,
                ApproverEmailSnapshot = approver.Email,
                IsRequired = rule.IsRequired,
                ParticipantStatus = ParticipantStatus.Pending,
                CreatedOnUtc = now,
                CreatedBy = currentUser.Username,
            });
        }

        db.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = instance.ServiceRequestId,
            EntryKind = HistoryEntryKind.Automation,
            EntryText = $"Approved at {step.StageNameSnapshot}; now with {next.StageNameSnapshot}.",
            OccurredOnUtc = now,
            PerformedBy = currentUser.Username,
        });

        if (resolved.Count == 0)
        {
            // Nobody to ask. The level stays Pending and nothing chases it, so
            // say so in the timeline rather than leaving the run to stall
            // silently. The administrator can cancel and fix the route.
            db.RequestHistories.Add(new RequestHistory
            {
                ServiceRequestId = instance.ServiceRequestId,
                EntryKind = HistoryEntryKind.Escalation,
                EntryText = $"No approver could be found for {next.StageNameSnapshot}.",
                OccurredOnUtc = now,
                PerformedBy = "Approval workflow",
            });
        }
    }

    private async Task FinishAsync(
        RequestApprovalInstance instance,
        string status,
        DateTime now,
        CancellationToken ct)
    {
        instance.Status = status;
        instance.CompletedOnUtc = now;
        instance.CurrentStageNumber = null;
        instance.ModifiedOnUtc = now;
        instance.ModifiedBy = currentUser.Username;

        // Whatever had not had its turn never will.
        var untouched = await db.RequestApprovalSteps
            .Where(s => s.RequestApprovalInstanceId == instance.Id
                && s.Status == ApprovalStepStatus.Waiting)
            .ToListAsync(ct);

        foreach (var step in untouched)
        {
            step.Status = ApprovalStepStatus.Cancelled;
            step.ModifiedOnUtc = now;
        }

        db.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = instance.ServiceRequestId,
            EntryKind = HistoryEntryKind.Automation,
            EntryText = $"Approval {status.ToLowerInvariant()}.",
            OccurredOnUtc = now,
            PerformedBy = currentUser.Username,
        });
    }

    /// <summary>
    /// The answer a replayed call gets: the decision already recorded, and
    /// where things stand now.
    /// </summary>
    private async Task<DecideApprovalResponse> ReplayAsync(
        RequestApprovalDecision decision,
        CancellationToken ct)
    {
        var step = await db.RequestApprovalSteps
            .AsNoTracking()
            .SingleAsync(s => db.RequestApprovalParticipants.Any(
                p => p.Id == decision.RequestApprovalParticipantId
                    && p.RequestApprovalStepId == s.Id), ct);

        var instance = await db.RequestApprovalInstances
            .AsNoTracking()
            .SingleAsync(i => i.Id == step.RequestApprovalInstanceId, ct);

        return new DecideApprovalResponse(
            decision.RequestApprovalParticipantId, decision.Decision, step.Status,
            instance.Status, instance.CurrentStageNumber, WasAlreadyDecided: true);
    }
}
