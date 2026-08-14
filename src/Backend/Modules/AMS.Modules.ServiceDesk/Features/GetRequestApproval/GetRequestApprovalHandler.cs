using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.GetRequestApproval;

/// <summary>
/// The approval run on one request. Catalogue: the approval panel on Request
/// Detail.
/// </summary>
/// <remarks>
/// The most recent run, not every run. A request whose approval was cancelled
/// and resubmitted has two, and the panel shows where it stands now; the
/// earlier ones are still there, and still evidence, but they are an audit
/// question rather than a screen.
/// </remarks>
public sealed class GetRequestApprovalHandler(ServiceDeskDbContext db)
    : IRequestHandler<GetRequestApprovalQuery, GetRequestApprovalResponse>
{
    public async Task<Result<GetRequestApprovalResponse>> HandleAsync(
        GetRequestApprovalQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var instance = await db.RequestApprovalInstances
            .AsNoTracking()
            .Where(i => i.ServiceRequestId == request.Id)
            .OrderByDescending(i => i.SubmittedOnUtc)
            .ThenByDescending(i => i.Id)
            .FirstOrDefaultAsync(ct);

        if (instance is null)
        {
            return Error.NotFound("RequestApproval", request.Id);
        }

        var steps = await db.RequestApprovalSteps
            .AsNoTracking()
            .Where(s => s.RequestApprovalInstanceId == instance.Id)
            .OrderBy(s => s.StageNumber)
            .ToListAsync(ct);

        var stepIds = steps.ConvertAll(s => s.Id);

        var participants = await db.RequestApprovalParticipants
            .AsNoTracking()
            .Where(p => stepIds.Contains(p.RequestApprovalStepId))
            .OrderBy(p => p.Id)
            .ToListAsync(ct);

        var participantIds = participants.ConvertAll(p => p.Id);

        var decisions = await db.RequestApprovalDecisions
            .AsNoTracking()
            .Where(d => participantIds.Contains(d.RequestApprovalParticipantId))
            .ToListAsync(ct);

        // One decision per participant — UX_RequestApprovalDecision_Participant
        // makes that a fact rather than an assumption.
        var decisionByParticipant = decisions.ToDictionary(d => d.RequestApprovalParticipantId);

        var participantsByStep = participants
            .GroupBy(p => p.RequestApprovalStepId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var stepRows = steps.ConvertAll(s => new GetRequestApprovalResponse.Step(
            s.Id,
            s.StageNumber,
            s.StageNameSnapshot,
            s.ApprovalModeSnapshot,
            s.Status,
            s.ActivatedOnUtc,
            s.DueOnUtc,
            s.CompletedOnUtc,
            s.OutcomeRemarks,
            participantsByStep.TryGetValue(s.Id, out var own)
                ? own.ConvertAll(p => new GetRequestApprovalResponse.Participant(
                    p.Id,
                    p.ApproverUserId,
                    p.ApproverEmployeeId,
                    p.ApproverNameSnapshot,
                    p.ApproverEmailSnapshot,
                    p.IsRequired,
                    p.ParticipantStatus,
                    decisionByParticipant.TryGetValue(p.Id, out var d)
                        ? new GetRequestApprovalResponse.Decision(
                            d.Id, d.Decision, d.Remarks, d.ActedByUserId,
                            d.ActedByEmailSnapshot, d.Source, d.DecidedOnUtc)
                        : null))
                : []));

        return new GetRequestApprovalResponse(
            instance.Id,
            instance.ServiceRequestId,
            instance.WorkflowNameSnapshot,
            instance.WorkflowVersion,
            instance.Status,
            instance.CurrentStageNumber,
            instance.SubmittedByUserId,
            instance.SubmittedOnUtc,
            instance.CompletedOnUtc,
            instance.CancelledOnUtc,
            instance.CancellationReason,
            stepRows);
    }
}
