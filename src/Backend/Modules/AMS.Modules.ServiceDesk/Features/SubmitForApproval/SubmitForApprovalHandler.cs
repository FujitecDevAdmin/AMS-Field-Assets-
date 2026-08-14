using AMS.Modules.ServiceDesk.Approvals;
using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.SubmitForApproval;

/// <summary>
/// Send a new service request for approval. Catalogue: Submit for Approval.
/// </summary>
/// <remarks>
/// <para>
/// This is where the whole run is built: the route is chosen, every level is
/// copied out of the definition into steps, the first level's approvers are
/// resolved and snapshotted, and that level is activated. Everything after
/// this point reads the snapshots — nothing re-reads the definition, so a
/// route edited tomorrow cannot change what today's run is being judged by.
/// </para>
/// <para>
/// Only the first level's approvers are resolved now. Later levels resolve as
/// their turn comes, because "the manager of the requester" a fortnight into a
/// long approval should be the manager then, not a name captured before
/// anybody had looked at it.
/// </para>
/// </remarks>
public sealed class SubmitForApprovalHandler(
    ServiceDeskDbContext db,
    ApproverResolver resolver,
    ApprovalNotifications notifications,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<SubmitForApprovalCommand, SubmitForApprovalResponse>
{
    public async Task<Result<SubmitForApprovalResponse>> HandleAsync(
        SubmitForApprovalCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ticket = await db.ServiceRequests.SingleOrDefaultAsync(r => r.Id == request.Id, ct);
        if (ticket is null)
        {
            return Error.NotFound("ServiceRequest", request.Id);
        }

        // Approval is a NewService thing. A printer fault does not go to a
        // manager, and a workflow attached to one would be a route nobody
        // configured on purpose.
        if (ticket.RequestKind != RequestKind.NewService)
        {
            return Error.Validation(
                "ServiceRequest.NotApprovable",
                "Only a new service request goes through approval.");
        }

        // UX_RequestApprovalInstance_OnePending would catch this too. Saying it
        // here means a double-click gets a sentence rather than a 409 about an
        // index.
        if (await db.RequestApprovalInstances.AnyAsync(
                i => i.ServiceRequestId == ticket.Id
                    && i.Status == ApprovalInstanceStatus.Pending, ct))
        {
            return Error.Conflict(
                "RequestApproval.AlreadyRunning",
                "This request is already waiting for approval.");
        }

        var definition = await ChooseWorkflowAsync(request.ApprovalWorkflowId, ticket, ct);
        if (definition is null)
        {
            return request.ApprovalWorkflowId is { } chosen
                ? Error.NotFound("ApprovalWorkflow", chosen)
                : Error.Validation(
                    "ApprovalWorkflow.NoneMatches",
                    "No published approval route matches this request, and there is no default.");
        }

        var stages = await db.ApprovalWorkflowStages
            .Where(s => s.ApprovalWorkflowId == definition.Id && s.IsEnabled)
            .OrderBy(s => s.StageNumber)
            .ToListAsync(ct);

        if (stages.Count == 0)
        {
            return Error.Validation(
                "ApprovalWorkflow.NoStages",
                "That route has no levels, so it would approve nothing.");
        }

        var ticketContext = new ApprovalContext(
            ticket.RequestedByEmployeeId, ticket.OnBehalfOfEmployeeId, ticket.LocationId);

        var rules = await db.ApprovalStageApproverRules
            .Where(r => r.ApprovalWorkflowStageId == stages[0].Id)
            .OrderBy(r => r.Id)
            .ToListAsync(ct);

        // Resolved BEFORE a single row is written. Writing the run first and
        // unwinding it on failure would work — the dispatcher owns one
        // transaction per command — but it would make this handler correct only
        // because of something outside it, and a run that started with nobody
        // to ask would sit Pending for ever if that ever stopped being true.
        var approvers = await resolver.ResolveAsync(rules, ticketContext, ct);

        if (approvers.Count == 0)
        {
            return Error.Validation(
                "RequestApproval.NoApprovers",
                $"No approver could be found for {stages[0].StageName}. "
                + "Check the route's rules and that those people have e-mail addresses.");
        }

        var now = clock.UtcNow;

        var instance = new RequestApprovalInstance
        {
            ServiceRequestId = ticket.Id,
            ApprovalWorkflowId = definition.Id,
            // Copied, not joined. A year from now the audit should read
            // without the definition still existing under the same name.
            WorkflowNameSnapshot = definition.WorkflowName,
            WorkflowVersion = definition.VersionNumber,
            Status = ApprovalInstanceStatus.Pending,
            CurrentStageNumber = stages[0].StageNumber,
            SubmittedByUserId = currentUser.Id,
            SubmittedOnUtc = now,
            CreatedOnUtc = now,
            CreatedBy = currentUser.Username,
        };

        db.RequestApprovalInstances.Add(instance);

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

        // Every level is written now, Waiting, so the panel can show what is
        // coming. CK_RequestApprovalStep_Activation allows Waiting to have no
        // activation time, which is exactly why it does.
        var steps = stages.ConvertAll(stage => new RequestApprovalStep
        {
            RequestApprovalInstanceId = instance.Id,
            ApprovalWorkflowStageId = stage.Id,
            StageNumber = stage.StageNumber,
            StageNameSnapshot = stage.StageName,
            ApprovalModeSnapshot = stage.ApprovalMode,
            Status = ApprovalStepStatus.Waiting,
            CreatedOnUtc = now,
            CreatedBy = currentUser.Username,
        });

        db.RequestApprovalSteps.AddRange(steps);
        await db.SaveChangesAsync(ct);

        var first = steps[0];
        ApprovalRun.Activate(first, stages[0], now);

        foreach (var (rule, approver) in approvers)
        {
            db.RequestApprovalParticipants.Add(new RequestApprovalParticipant
            {
                RequestApprovalStepId = first.Id,
                ApproverRuleId = rule.Id,
                ApproverUserId = approver.UserId,
                ApproverEmployeeId = approver.EmployeeId,
                // Snapshots. A leaver, a rename, a new address: none of them
                // may rewrite who was asked.
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
            ServiceRequestId = ticket.Id,
            EntryKind = HistoryEntryKind.Automation,
            EntryText = $"Submitted for approval: {definition.WorkflowName} v{definition.VersionNumber}.",
            OccurredOnUtc = now,
            PerformedBy = currentUser.Username,
        });

        ticket.ModifiedOnUtc = now;
        ticket.ModifiedBy = currentUser.Username;

        await db.SaveChangesAsync(ct);

        // And now tell them. Resolving approvers and never asking them is an
        // approval that waits for ever, which is what this did until the
        // Notifications module existed to ask through.
        await notifications.AskAsync(instance, first, ticket, ct);

        return new SubmitForApprovalResponse(
            instance.Id, ticket.Id, instance.WorkflowNameSnapshot, instance.WorkflowVersion,
            instance.Status, instance.CurrentStageNumber, approvers.Count);
    }

    /// <summary>
    /// The route this request runs through: the one asked for, the one that
    /// matches, or the default.
    /// </summary>
    /// <remarks>
    /// Matching is most-specific-first — a route naming this template, branch
    /// and priority beats one naming only the template, which beats the
    /// default. A NULL column means "any", so a route with three NULLs matches
    /// everything and is what a default usually is.
    /// </remarks>
    private async Task<ApprovalWorkflowDefinition?> ChooseWorkflowAsync(
        int? chosenId,
        ServiceRequest ticket,
        CancellationToken ct)
    {
        var live = db.ApprovalWorkflowDefinitions.Where(w => w.IsActive && w.IsPublished);

        if (chosenId is { } id)
        {
            return await live.SingleOrDefaultAsync(w => w.Id == id, ct);
        }

        var now = clock.UtcNow;

        var candidates = await live
            .Where(w => w.EffectiveFromUtc == null || w.EffectiveFromUtc <= now)
            .Where(w => w.EffectiveToUtc == null || w.EffectiveToUtc > now)
            .Where(w => w.ServiceTemplateId == null || w.ServiceTemplateId == ticket.ServiceTemplateId)
            .Where(w => w.LocationId == null || w.LocationId == ticket.LocationId)
            .Where(w => w.Priority == null || w.Priority == ticket.Priority)
            .ToListAsync(ct);

        return candidates
            .OrderByDescending(w => Specificity(w))
            .ThenByDescending(w => w.VersionNumber)
            .FirstOrDefault()
            ?? await live.FirstOrDefaultAsync(w => w.IsDefault, ct);
    }

    private static int Specificity(ApprovalWorkflowDefinition w) =>
        (w.ServiceTemplateId is null ? 0 : 4)
        + (w.LocationId is null ? 0 : 2)
        + (w.Priority is null ? 0 : 1);

}
